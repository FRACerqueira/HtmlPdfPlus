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
                return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ErrorInfo.FromException(ex));
            }
            var isurl = requestHtmlPdf.Mode == RenderMode.Url;
            return await RunServer(isurl,null,null,sw, requestHtmlPdf, PdfSrvBuilder.DisableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress), token);
        }

        internal async Task<HtmlPdfResult<Tout>> RunServer(
            bool isurl,
            Func<string, Tin?, CancellationToken, Task<string>>? inputparam,
            Func<byte[]?, Tin?, CancellationToken, Task<Tout>>? outputparam,
            Stopwatch sw, 
            RequestHtmlPdf<Tin> requestHtmlPdf,
            bool disableCompress, 
            CancellationToken token = default)
        {
            if (inputparam is not null)
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(requestHtmlPdf.Timeout);
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
                    var backstop = Task.Delay(requestHtmlPdf.Timeout, token);
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

            var reamaindtime = requestHtmlPdf.Timeout - sw.ElapsedMilliseconds;
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
                        return new HtmlPdfResult<Tout>(false, true, sw.Elapsed, default, new ErrorInfo(ErrorCode.PoolExhausted, "Not AvailableBuffer", retryable: true));
                    }
                    if (bytespdf.Length == 0)
                    {
                        return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new ErrorInfo(ErrorCode.Timeout, $"Reached Timeout({requestHtmlPdf.Timeout})", retryable: true));
                    }
                    LogMessage($"Executed the Generate PDF after {sw.Elapsed}");
                }
                catch (Exception ex)
                {
                    cts.Cancel(); // cancel pending task
                    LogMessage($"Error Generate PDF from browser after {sw.Elapsed} : {ex}");
                    return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ErrorInfo.FromException(ex));
                }
            }

            if (outputparam is not null)
            {
                reamaindtime = requestHtmlPdf.Timeout - sw.ElapsedMilliseconds;
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
                        var aux = await outputparam(bytespdf, requestHtmlPdf.InputParam, executeToken.Token);
                        if (typeof(Tout) == typeof(byte[]))
                        {
                            if (!disableCompress)
                            {
                                result = new HtmlPdfResult<Tout>(true, false, sw.Elapsed, aux, null);
                            }
                            else
                            {
                                var compresspdf = await GZipHelper.CompressAsync((byte[])(object)aux!,token);
                                result = new HtmlPdfResult<Tout>(true, false, sw.Elapsed, (Tout)(object)compresspdf, null);
                            }
                        }
                        else
                        {
                            result = new HtmlPdfResult<Tout>(true, false, sw.Elapsed, aux, null);
                        }
                    }, executeToken.Token);

                    // Same rationale as the BeforePDF backstop above: decoupled from
                    // cts/executeToken so it can't race taskoutput off the same cancellation
                    // event, while still guaranteeing a bounded wait.
                    var backstop = Task.Delay(requestHtmlPdf.Timeout, token);
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
            //output is byte[]
            if (!disableCompress)
            {
                bytespdf = await GZipHelper.CompressAsync(bytespdf, token);
            }
            LogMessage($"End Convert Html to PDF from Server at {DateTime.Now} after {sw.Elapsed}");
            return new HtmlPdfResult<Tout>(true, false, sw.Elapsed, (Tout)(object)bytespdf, null);
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
                        resultpdf = [];
                    }
                }
            }
            catch (OperationCanceledException)
            {
                resultpdf = [];
            }
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
                        // that is still in use underneath it.
                        await PdfSrvBuilder!.ReplenishBufferAsync();
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
            }
        }

        // Reusable logging
        private static readonly Action<ILogger, string, string, Exception?> logMessageForInf = LoggerMessage.Define<string, string>(LogLevel.Information, 0, "HtmlPdfSrvPlus({Source}) : {Message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForTrc = LoggerMessage.Define<string, string>(LogLevel.Trace, 0, "HtmlPdfSrvPlus({Source}) : {Message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForDbg = LoggerMessage.Define<string, string>(LogLevel.Debug, 0, "HtmlPdfSrvPlus({Source}) : {Message}");
    }
}
