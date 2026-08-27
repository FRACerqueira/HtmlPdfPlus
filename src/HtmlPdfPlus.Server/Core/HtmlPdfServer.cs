// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using HtmlPdfPlus.Shared.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace HtmlPdfPlus.Server.Core
{
    /// <summary>
    /// Represents a server for converting HTML to PDF.
    /// </summary>
    /// <typeparam name="Tin">The type of input data.</typeparam>
    /// <typeparam name="Tout">The type of output data.</typeparam>
    internal sealed class HtmlPdfServer<Tin, Tout> : IHtmlPdfServer<Tin, Tout>
    {
        private bool isDisposed;

        // Beyond this, an apparent RequestHtmlPdf.SentAtUtc-derived transit is treated as
        // clock skew between client and server rather than genuine network/queueing time.
        private const int MaxPlausibleTransitMs = 5000;

        internal readonly HtmlPdfBuilder PdfSrvBuilder;
        internal readonly string SourceAlias;

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlPdfServer{Tin, Tout}"/> class.
        /// </summary>
        /// <param name="pdfSrvBuilder">The PDF service builder.</param>
        /// <param name="sourcealias">The source alias.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pdfSrvBuilder"/> is null.</exception>
#pragma warning disable IDE0290 // Use primary constructor
        public HtmlPdfServer(HtmlPdfBuilder? pdfSrvBuilder, string sourcealias)
        {
            PdfSrvBuilder = pdfSrvBuilder ?? throw new ArgumentNullException(nameof(pdfSrvBuilder), "The pdfSrvBuilder is null");
            SourceAlias = sourcealias;
        }
#pragma warning restore IDE0290 // Use primary constructor


        /// <summary>
        /// Reports whether this instance's browser/pool can currently accept and process requests.
        /// </summary>
        internal HtmlPdfHealthStatus GetHealthStatus()
        {
            return new HtmlPdfHealthStatus(
                PdfSrvBuilder.CurrentBrowser?.IsConnected ?? false,
                PdfSrvBuilder.IsRecovering,
                PdfSrvBuilder.BufferLength,
                PdfSrvBuilder.IsPoolStarved);
        }

        /// <inheritdoc />
        public IHtmlPdfServerContext<Tin, Tout> ScopeData(Tin? inputparam)
        {
            return new HtmlPdfServerContext<Tin, Tout>(this, inputparam, null);
        }

        /// <inheritdoc />
        public IHtmlPdfServerContext<Tin, Tout> ScopeRequest(byte[] requestClient)
        {
            return new HtmlPdfServerContext<Tin, Tout>(this, default, requestClient);
        }

        /// <inheritdoc />
        public async Task<HtmlPdfResult<Tout>> Run(byte[] requestclient, CancellationToken token = default)
        {
            if (requestclient is null || requestclient.Length ==0)
            {
                throw new ArgumentNullException(nameof(requestclient), "request client is null or empty");
            }
            var sw = Stopwatch.StartNew();
            RequestHtmlPdf<Tin> requestHtmlPdf;
            try
            {
                string data;
                if (PdfSrvBuilder.DisableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress))
                {
                    data = Encoding.UTF8.GetString(requestclient);
                }
                else
                {
                    data = Encoding.UTF8.GetString(await GZipHelper.DecompressAsync(requestclient, PdfSrvBuilder.MaxDecompressedRequestSizeLimit, token));
                    LogMessage($"Decompress Request after {sw.Elapsed}");
                }
                requestHtmlPdf = JsonSerializer.Deserialize<RequestHtmlPdf<Tin>>(data, GZipHelper.JsonOptions)!;
                requestHtmlPdf.Config ??= PdfSrvBuilder.Config;

                if (requestHtmlPdf.Timeout < 1)
                {
                    throw new ArgumentException("Timeout must be greater than zero");
                }
                if (string.IsNullOrEmpty(requestHtmlPdf.Html))
                {
                    throw new ArgumentException("Html is null or empty");
                }
                if (requestHtmlPdf.Config!.Scale < 0.1 || requestHtmlPdf.Config!.Scale > 2)
                {
                    throw new ArgumentException("Scale amount must be between 0.1 and 2.");
                }
            }
            catch (Exception ex)
            {
                // Unlike RequestDuration (meaningless for a request that was never actually
                // attempted), a validation failure is still a real failure a host's error-rate
                // alerting needs to see - recording only errors that survive to RunServer would
                // make this counter go silent under a flood of malformed requests.
                var validationFailure = new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ErrorInfo.FromException(ex));
                RecordErrorIfAny(validationFailure);
                return validationFailure;
            }
            var isurl = requestHtmlPdf.Mode == RenderMode.Url;
            var result = await RunServer(isurl,null,null,sw, requestHtmlPdf, token);
            RecordRequestDuration(result);
            RecordErrorIfAny(result);
            return result;
        }

        /// <summary>
        /// Records the request-duration metric (see <see cref="HtmlPdfMetrics.RequestDuration"/>),
        /// tagged with this instance's source alias and whether the request succeeded.
        /// </summary>
        internal void RecordRequestDuration(HtmlPdfResult<Tout> result)
        {
            HtmlPdfMetrics.RequestDuration.Record(
                result.ElapsedTime.TotalMilliseconds,
                new KeyValuePair<string, object?>("sourcealias", SourceAlias),
                new KeyValuePair<string, object?>("success", result.IsSuccess));
        }

        /// <summary>
        /// Increments the error-count metric (see <see cref="HtmlPdfMetrics.Errors"/>) when
        /// <paramref name="result"/> is a failure, tagged with this instance's source alias and
        /// the failure's <see cref="ErrorCode"/>. A no-op for a successful result.
        /// </summary>
        internal void RecordErrorIfAny(HtmlPdfResult<Tout> result)
        {
            if (result.IsSuccess)
            {
                return;
            }
            HtmlPdfMetrics.Errors.Add(1,
                new KeyValuePair<string, object?>("sourcealias", SourceAlias),
                new KeyValuePair<string, object?>("error_code", result.Error!.Code.ToString()));
        }

        internal async Task<HtmlPdfResult<Tout>> RunServer(
            bool isurl,
            Func<string, Tin?, CancellationToken, Task<string>>? inputparam,
            Func<byte[]?, Tin?, CancellationToken, Task<Tout>>? outputparam,
            Stopwatch sw,
            RequestHtmlPdf<Tin> requestHtmlPdf,
            CancellationToken token = default)
        {
            // requestHtmlPdf.Timeout is relative to when the client sent the request, not to
            // when this method starts running - subtract time already spent in transit
            // (network, queueing) so the deadline is honored end-to-end instead of being
            // restarted fresh on arrival. SentAtUtc is null for in-process requests, which have
            // no transport hop to account for.
            var effectiveTimeout = requestHtmlPdf.Timeout;
            if (requestHtmlPdf.SentAtUtc is DateTimeOffset sentAtUtc)
            {
                var transitMs = (DateTimeOffset.UtcNow - sentAtUtc).TotalMilliseconds;
                if (transitMs > MaxPlausibleTransitMs)
                {
                    // An implausibly large "transit" is far more likely to be clock skew
                    // between client and server than a genuine multi-second network hop -
                    // trusting it would let a skewed clock fail every request outright. Ignore
                    // SentAtUtc for this request instead of treating skew as an exhausted
                    // deadline; a negative transit (server clock behind client) is likewise
                    // clamped to zero rather than extending the budget.
                    LogMessage($"Ignoring SentAtUtc: implausible transit ({transitMs:F0}ms) - possible clock skew between client and server");
                }
                else
                {
                    effectiveTimeout = (int)Math.Max(0, requestHtmlPdf.Timeout - Math.Max(0, transitMs));
                    if (effectiveTimeout == 0)
                    {
                        LogMessage($"Reached Timeout({requestHtmlPdf.Timeout}) - {transitMs:F0}ms already spent in transit before processing began");
                        return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new ErrorInfo(ErrorCode.Timeout, $"Reached Timeout({requestHtmlPdf.Timeout})", retryable: true));
                    }
                }
            }

            if (inputparam is not null)
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(effectiveTimeout);
                using var executeToken = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
                try
                {
                    var taskinput = Task.Run(async () =>
                    {
                        requestHtmlPdf.ChangeHtml(await inputparam(requestHtmlPdf.Html, requestHtmlPdf.InputParam, executeToken.Token),
                            isurl || PdfSrvBuilder.DisableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableMinifyHtml));
                    }, executeToken.Token);

                    // Backstop driven only by elapsed time and the caller's own token, never
                    // by cts/executeToken - taskinput is ALSO driven by executeToken, so racing
                    // it against a delay built from the same token let a Canceled taskinput
                    // "win" silently: IsFaulted missed it, no branch returned, and execution
                    // fell through to PDF generation as if BeforePDF had succeeded.
                    var backstop = Task.Delay(effectiveTimeout, token);
                    var completed = await Task.WhenAny(taskinput, backstop);
                    if (completed == backstop)
                    {
                        LogMessage($"Reached Timeout({requestHtmlPdf.Timeout})");
                        return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new ErrorInfo(ErrorCode.Timeout, $"Reached Timeout({requestHtmlPdf.Timeout})", retryable: true));
                    }
                    // Observe taskinput so a fault or a cancellation raised by the delegate
                    // itself surfaces as a real exception below via the catch blocks (which
                    // already build ErrorInfo correctly), instead of being inferred (and
                    // possibly missed, e.g. a Canceled task has a null Exception) from Task
                    // state directly.
                    await taskinput;
                    LogMessage($"Executed the BeforePDF function after {sw.Elapsed}");
                }
                catch (OperationCanceledException ex)
                {
                    if (cts.IsCancellationRequested)
                    {
                        LogMessage($"Reached Timeout({requestHtmlPdf.Timeout})");
                        return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new ErrorInfo(ErrorCode.Timeout, $"Reached Timeout({requestHtmlPdf.Timeout})", retryable: true));
                    }
                    else
                    {
                        LogMessage($"Canceled by token server");
                        return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ErrorInfo.FromException(ex));
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error BeforePDF function after {sw.Elapsed} : {ex}");
                    return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ErrorInfo.FromException(ex));
                }
                finally
                {
                    cts.Cancel(); // cancel pending task  
                }
            }

            var reamaindtime = effectiveTimeout - sw.ElapsedMilliseconds;
            if (reamaindtime < 0)
            {
                reamaindtime = 0;
            }

            byte[]? bytespdf;

            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromMilliseconds(reamaindtime));

                using var executeToken = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
                try
                {
                    bytespdf = await GeneratePDF(isurl, requestHtmlPdf, reamaindtime, executeToken.Token);
                    if (bytespdf is null)
                    {
                        // The pool didn't free a page within AcquireTimeoutMs - suggest waiting
                        // roughly that long again, since a shorter retry would likely race into
                        // the same exhaustion.
                        var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(PdfSrvBuilder!.AcquireTimeoutMs / 1000.0));
                        return new HtmlPdfResult<Tout>(false, true, sw.Elapsed, default, new ErrorInfo(ErrorCode.PoolExhausted, "Not AvailableBuffer", retryable: true, retryAfterSeconds));
                    }
                    if (bytespdf.Length == 0)
                    {
                        return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new ErrorInfo(ErrorCode.Timeout, $"Reached Timeout({requestHtmlPdf.Timeout})", retryable: true));
                    }
                    LogMessage($"Executed the Generate PDF after {sw.Elapsed}");
                }
                catch (OperationCanceledException ex)
                {
                    // Reached here only when AcquireAsync's own AcquireTimeoutMs was NOT what
                    // fired (that case returns null above, reported as PoolExhausted) - so this
                    // is the overall deadline or an external cancellation, same as BeforePDF/AfterPDF.
                    var reachedOverallTimeout = cts.IsCancellationRequested;
                    cts.Cancel(); // cancel pending task
                    if (reachedOverallTimeout)
                    {
                        LogMessage($"Reached Timeout({requestHtmlPdf.Timeout})");
                        return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new ErrorInfo(ErrorCode.Timeout, $"Reached Timeout({requestHtmlPdf.Timeout})", retryable: true));
                    }
                    LogMessage($"Canceled by token server");
                    return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ErrorInfo.FromException(ex));
                }
                catch (Exception ex)
                {
                    cts.Cancel(); // cancel pending task
                    LogMessage($"Error Generate PDF from browser after {sw.Elapsed} : {ex}");
                    return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ClassifyGeneratePdfException(ex));
                }
            }

            if (outputparam is not null)
            {
                reamaindtime = effectiveTimeout - sw.ElapsedMilliseconds;
                if (reamaindtime < 0)
                {
                    reamaindtime = 0;
                }
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromMilliseconds(reamaindtime));
                using var executeToken = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
                HtmlPdfResult<Tout>? result = null;
                try
                {
                    var taskoutput = Task.Run(async () =>
                    {
                        // A byte[] output is the PDF itself and is never app-level compressed -
                        // it travels over the wire as the raw response body (see RunServer's
                        // no-AfterPDF path below for the same rationale).
                        var aux = await outputparam(bytespdf, requestHtmlPdf.InputParam, executeToken.Token);
                        result = new HtmlPdfResult<Tout>(true, false, sw.Elapsed, aux, null);
                    }, executeToken.Token);

                    // Same rationale as the BeforePDF backstop above: decoupled from
                    // cts/executeToken so it can't race taskoutput off the same cancellation
                    // event, while still guaranteeing a bounded wait.
                    var backstop = Task.Delay(effectiveTimeout, token);
                    var completed = await Task.WhenAny(taskoutput, backstop);
                    if (completed == backstop)
                    {
                        LogMessage($"Reached Timeout({requestHtmlPdf.Timeout})");
                        result = new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new ErrorInfo(ErrorCode.Timeout, $"Reached Timeout({requestHtmlPdf.Timeout})", retryable: true));
                    }
                    else
                    {
                        // Observe taskoutput so a fault or a cancellation raised by the
                        // delegate itself surfaces as a real exception below via the catch
                        // blocks (which already build ErrorInfo correctly), instead of being
                        // inferred (and possibly missed, e.g. a Canceled task has a null
                        // Exception) from Task state directly.
                        await taskoutput;
                        LogMessage($"Executed the AfterPDF function after {sw.Elapsed}");
                    }
                }
                catch (OperationCanceledException ex)
                {
                    if (cts.IsCancellationRequested)
                    {
                        LogMessage($"Reached Timeout({requestHtmlPdf.Timeout})");
                        result = new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new ErrorInfo(ErrorCode.Timeout, $"Reached Timeout({requestHtmlPdf.Timeout})", retryable: true));
                    }
                    else
                    {
                        LogMessage($"Canceled by token server");
                        result = new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ErrorInfo.FromException(ex));
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error AfterPDF function after {sw.Elapsed} : {ex}");
                    result = new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ErrorInfo.FromException(ex));
                }
                finally
                {
                    cts.Cancel(); // cancel pending task  
                }
                LogMessage($"End Convert Html to PDF from Server with AfterPDF function at {DateTime.Now} after {sw.Elapsed}");
                return result!;
            }
            //output is byte[] - the PDF itself, served as raw bytes, never app-level compressed
            //(transport compression, if any, is standard Content-Encoding, not GZipHelper here).
            LogMessage($"End Convert Html to PDF from Server at {DateTime.Now} after {sw.Elapsed}");
            return new HtmlPdfResult<Tout>(true, false, sw.Elapsed, (Tout)(object)bytespdf, null);
        }

        /// <summary>
        /// Classifies an exception raised while navigating/rendering (<see cref="GeneratePDF"/>).
        /// A live Playwright/browser failure (the page or its browser died mid-render - the exact
        /// condition <see cref="HtmlPdfBuilder"/>'s automatic recovery exists for) is reported as
        /// <see cref="ErrorCode.RenderFailed"/> and retryable, instead of falling through
        /// <see cref="ErrorInfo.FromException(Exception)"/>'s default arm to a non-retryable
        /// <see cref="ErrorCode.Internal"/> that gives a caller no reason to try again. Every other
        /// failure kind keeps the generic classification.
        /// </summary>
        internal static ErrorInfo ClassifyGeneratePdfException(Exception ex)
        {
            return ex is PlaywrightException
                ? new ErrorInfo(ErrorCode.RenderFailed, ex.Message, retryable: true)
                : ErrorInfo.FromException(ex);
        }

        private async Task<byte[]?> GeneratePDF(bool isurl, RequestHtmlPdf<Tin> request, long remaindtime, CancellationToken token)
        {
            IPage? page = null;
            byte[] resultpdf = [];
            Task? taskpdf = null;
            try
            {
                page = await PdfSrvBuilder!.AcquireAsync(token).ConfigureAwait(false);
                if (page == null)
                {
                    return null;
                }
                if (isurl)
                {
                    if (!Uri.TryCreate(request.Html, UriKind.Absolute, out var targeturi))
                    {
                        throw new InvalidOperationException($"RenderMode.Url requires an absolute URL: '{request.Html}'");
                    }
                    if (!PdfSrvBuilder!.IsUrlAllowed(targeturi))
                    {
                        throw new InvalidOperationException($"The URL was rejected by the configured URL policy: '{request.Html}'");
                    }
                    await page.GotoAsync(request.Html, new PageGotoOptions
                    {
                        Timeout = remaindtime,
                        WaitUntil = WaitUntilState.DOMContentLoaded
                    });

                }
                else
                {
                    await page.SetContentAsync(request.Html, new PageSetContentOptions
                    {
                        Timeout = remaindtime,
                        WaitUntil = WaitUntilState.DOMContentLoaded
                    });
                }
                taskpdf = Task.Run(async () =>
                {
                    resultpdf = await page.PdfAsync(new PagePdfOptions
                    {
                        HeaderTemplate = request.Config!.Header,
                        FooterTemplate = request.Config!.Footer,
                        Height = request.Config.Size.Height.ToString("0.0mm", CultureInfo.InvariantCulture),
                        Width = request.Config.Size.Width.ToString("0.0mm", CultureInfo.InvariantCulture),
                        Landscape = request.Config.Orientation == PageOrientation.Landscape,
                        Margin = new Margin
                        {
                            Top = request.Config.Margins.Top.ToString("0.0mm", CultureInfo.InvariantCulture),
                            Bottom = request.Config.Margins.Bottom.ToString("0.0mm", CultureInfo.InvariantCulture),
                            Left = request.Config.Margins.Left.ToString("0.0mm", CultureInfo.InvariantCulture),
                            Right = request.Config.Margins.Right.ToString("0.0mm", CultureInfo.InvariantCulture)
                        },
                        DisplayHeaderFooter = request.Config.DisplayHeaderFooter,
                        PrintBackground = request.Config.PrintBackground,
                        PreferCSSPageSize = request.Config.PreferCSSPageSize,
                        Scale = request.Config.Scale
                    });
                }, token);

                var completed = await Task.WhenAny(taskpdf, Task.Delay(TimeSpan.FromMilliseconds(remaindtime), token));
                if (completed != taskpdf)
                {
                    resultpdf = [];
                }
                else
                {
                    if (taskpdf.IsFaulted)
                    {
                        // The browser/page died mid-render (e.g. a Chromium crash) - throw the real
                        // exception instead of silently collapsing to an empty result, so it reaches
                        // the catch below and gets classified via ClassifyGeneratePdfException
                        // (PlaywrightException -> RenderFailed/retryable) deterministically, instead
                        // of racily depending on whether a downstream pool-cleanup call happens to
                        // also throw before generation gets bumped.
                        throw taskpdf.Exception!.GetBaseException();
                    }
                }
            }
            // No catch (OperationCanceledException) here: AcquireAsync only lets one through
            // when it was NOT its own AcquireTimeoutMs that fired (see its callsite comment),
            // i.e. the caller's overall deadline or an external cancellation - that must reach
            // RunServer's own try/catch around this call so it can be classified as Timeout vs
            // Canceled the same way BeforePDF/AfterPDF already are, instead of being collapsed
            // into an empty result (which RunServer would otherwise always report as Timeout).
            finally
            {
                if (page is not null)
                {
                    if (taskpdf is not null && !taskpdf.IsCompleted)
                    {
                        // Playwright's PdfAsync takes no cancellation token, so the timeout
                        // above could not abort it - it may still be running on this page.
                        // Replenish the pool immediately with a fresh page, and only close
                        // this one once that work actually settles, instead of closing a page
                        // that is still in use underneath it. ReplenishIfCurrentGenerationAsync
                        // skips the replenish if this page's browser generation has since
                        // crashed and been replaced (recovery's own refill already restored
                        // that capacity - replenishing here too would silently overshoot
                        // PagesBuffer, the same reason RestoreAvailableBuffer gates its own
                        // replenish call). It never throws, so CloseWhenSettled always runs
                        // regardless of whether the replenish itself ran or succeeded - a
                        // pool-bookkeeping failure here must not leave this still-running page
                        // permanently unscheduled for cleanup, nor override whatever exception
                        // is already propagating from the try block.
                        await PdfSrvBuilder!.ReplenishIfCurrentGenerationAsync(page);
                        PdfSrvBuilder!.CloseWhenSettled(page, taskpdf);
                    }
                    else
                    {
                        await PdfSrvBuilder!.RestoreAvailableBuffer(page);
                    }
                }
            }
            return resultpdf;
        }

        /// <summary>
        /// Clean-up code is implemented
        /// </summary>
        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            Cleanup();
            GC.SuppressFinalize(this);
        }

        private void Cleanup()
        {
            PdfSrvBuilder?.Dispose();
        }

        private void LogMessage(string message)
        {
            if (PdfSrvBuilder is null || PdfSrvBuilder.Log is null || (!PdfSrvBuilder.Log?.IsEnabled(PdfSrvBuilder.LevelLog) ?? false)) return;

            switch (PdfSrvBuilder.LevelLog)
            {
                case LogLevel.None:
                    return;
                case LogLevel.Trace:
                    logMessageForTrc(PdfSrvBuilder.Log!, SourceAlias, message, null);
                    break;
                case LogLevel.Information:
                    logMessageForInf(PdfSrvBuilder.Log!, SourceAlias, message, null);
                    break;
                case LogLevel.Debug:
                    logMessageForDbg(PdfSrvBuilder.Log!, SourceAlias, message, null);
                    break;
                case LogLevel.Warning:
                    logMessageForWrn(PdfSrvBuilder.Log!, SourceAlias, message, null);
                    break;
                case LogLevel.Error:
                    logMessageForErr(PdfSrvBuilder.Log!, SourceAlias, message, null);
                    break;
                case LogLevel.Critical:
                    logMessageForCrt(PdfSrvBuilder.Log!, SourceAlias, message, null);
                    break;
            }
        }

        // Reusable logging
        private static readonly Action<ILogger, string, string, Exception?> logMessageForInf = LoggerMessage.Define<string, string>(LogLevel.Information, 0, "HtmlPdfSrvPlus({Source}) : {Message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForTrc = LoggerMessage.Define<string, string>(LogLevel.Trace, 0, "HtmlPdfSrvPlus({Source}) : {Message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForDbg = LoggerMessage.Define<string, string>(LogLevel.Debug, 0, "HtmlPdfSrvPlus({Source}) : {Message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForWrn = LoggerMessage.Define<string, string>(LogLevel.Warning, 0, "HtmlPdfSrvPlus({Source}) : {Message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForErr = LoggerMessage.Define<string, string>(LogLevel.Error, 0, "HtmlPdfSrvPlus({Source}) : {Message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForCrt = LoggerMessage.Define<string, string>(LogLevel.Critical, 0, "HtmlPdfSrvPlus({Source}) : {Message}");
    }
}
