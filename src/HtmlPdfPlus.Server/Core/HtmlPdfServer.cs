// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Diagnostics;
using System.Globalization;
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
        public IHtmlPdfServerContext<Tin, Tout> ScopeRequest(string requestClient)
        {
            return new HtmlPdfServerContext<Tin, Tout>(this, default, requestClient);
        }

        /// <inheritdoc />
        public async Task<HtmlPdfResult<Tout>> Run(string requestclient, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(requestclient))
            {
                throw new ArgumentNullException(nameof(requestclient), "request client is null or empty");
            }
            var sw = Stopwatch.StartNew();
            RequestHtmlPdf<Tin> requestHtmlPdf;
            try
            {
                if (PdfSrvBuilder.DisableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress))
                {
                    requestHtmlPdf = JsonSerializer.Deserialize<RequestHtmlPdf<Tin>>(requestclient, GZipHelper.JsonOptions)!;
                }
                else
                {
                    requestHtmlPdf = GZipHelper.DecompressRequest<Tin>(requestclient);
                    LogMessage($"Decompress Request after {sw.Elapsed}");
                }
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
                return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ex);
            }
            var isurl = Uri.IsWellFormedUriString(requestHtmlPdf.Html, UriKind.RelativeOrAbsolute);
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

                    var completed = await Task.WhenAny(taskinput, Task.Delay(requestHtmlPdf.Timeout, executeToken.Token));
                    if (completed != taskinput)
                    {
                        LogMessage($"Reached Timeout({requestHtmlPdf.Timeout})");
                        return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new TimeoutException($"Reached Timeout(({requestHtmlPdf.Timeout})"));
                    }
                    else
                    {
                        if (taskinput.IsFaulted)
                        {
                            LogMessage($"Error BeforePDF function after {sw.Elapsed} : {taskinput.Exception.InnerException}");
                            return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, taskinput.Exception.InnerException);
                        }
                        else
                        {
                            LogMessage($"Executed the BeforePDF function after {sw.Elapsed}");
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    if (cts.IsCancellationRequested)
                    {
                        LogMessage($"Reached Timeout({requestHtmlPdf.Timeout})");
                        return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new TimeoutException($"Reached Timeout(({requestHtmlPdf.Timeout})"));
                    }
                    else
                    {
                        LogMessage($"Canceled by token server");
                        return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ex);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error BeforePDF function after {sw.Elapsed} : {ex}");
                    return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ex);
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
                        return new HtmlPdfResult<Tout>(false, true, sw.Elapsed, default, new InvalidOperationException("Not AvailableBuffer"));
                    }
                    if (bytespdf.Length == 0)
                    {
                        return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new TimeoutException($"Reached Timeout(({requestHtmlPdf.Timeout})"));
                    }
                    LogMessage($"Executed the Generate PDF after {sw.Elapsed}");
                }
                catch (Exception ex)
                {
                    cts.Cancel(); // cancel pending task  
                    LogMessage($"Error Generate PDF from serverless browser after {sw.Elapsed} : {ex}");
                    return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ex);
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
                            if (disableCompress)
                            {
                                result = new HtmlPdfResult<Tout>(true, false, sw.Elapsed, aux, null);
                            }
                            else
                            {
                                var auxBytes = (byte[]?)Convert.ChangeType(aux, typeof(byte[])) ??
                                    throw new InvalidOperationException("Conversion to byte[] resulted in null");
                                var compresspdf = GZipHelper.Compress(auxBytes);
                                result = new HtmlPdfResult<Tout>(true, false, sw.Elapsed, (Tout?)Convert.ChangeType(compresspdf, typeof(Tout)), null);
                            }
                        }
                        else
                        {
                            result = new HtmlPdfResult<Tout>(true, false, sw.Elapsed, aux, null);
                        }
                    }, executeToken.Token);

                    var completed = await Task.WhenAny(taskoutput, Task.Delay(requestHtmlPdf.Timeout, executeToken.Token));
                    if (completed != taskoutput)
                    {
                        LogMessage($"Reached Timeout({requestHtmlPdf.Timeout})");
                        result = new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new TimeoutException($"Reached Timeout(({requestHtmlPdf.Timeout})"));
                    }
                    else
                    {
                        if (taskoutput.IsFaulted)
                        {
                            LogMessage($"Error AfterPDF function after {sw.Elapsed} : {taskoutput.Exception.InnerException}");
                            result = new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, taskoutput.Exception.InnerException);
                        }
                        else
                        {
                            LogMessage($"Executed the AfterPDF function after {sw.Elapsed}");
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    if (cts.IsCancellationRequested)
                    {
                        LogMessage($"Reached Timeout({requestHtmlPdf.Timeout})");
                        result = new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new TimeoutException($"Reached Timeout(({requestHtmlPdf.Timeout})"));
                    }
                    else
                    {
                        LogMessage($"Canceled by token server");
                        result = new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ex);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error AfterPDF function after {sw.Elapsed} : {ex}");
                    result = new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ex);
                }
                finally
                {
                    cts.Cancel(); // cancel pending task  
                }
                LogMessage($"End Convert Html to PDF from Server with AfterPDF function at {DateTime.Now} after {sw.Elapsed}");
                return result!;
            }
            LogMessage($"End Convert Html to PDF from Server at {DateTime.Now} after {sw.Elapsed}");
            if (disableCompress)
            {
                return new HtmlPdfResult<Tout>(true, false, sw.Elapsed, (Tout?)Convert.ChangeType(bytespdf, typeof(Tout)), null);
            }
            var compresspdf = GZipHelper.Compress(bytespdf);
            return new HtmlPdfResult<Tout>(true, false, sw.Elapsed, (Tout?)Convert.ChangeType(compresspdf, typeof(Tout)), null);
        }

        private async Task<byte[]?> GeneratePDF(bool isurl, RequestHtmlPdf<Tin> request, long remaindtime, CancellationToken token)
        {
            IPage? page = null;
            byte[] resultpdf = [];
            try
            {
                page = PdfSrvBuilder!.Acquire(token);
                if (page == null)
                {
                    return null;
                }
                if (isurl)
                {
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
                var taskpdf = Task.Run(async () =>
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
                    await PdfSrvBuilder!.RestoreAvailableBuffer(page!);
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
        private static readonly Action<ILogger, string, string, Exception?> logMessageForInf = LoggerMessage.Define<string, string>(LogLevel.Information, 0, "HtmlPdfSrvPlus({source}) : {message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForTrc = LoggerMessage.Define<string, string>(LogLevel.Trace, 0, "HtmlPdfSrvPlus({source}) : {message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForDbg = LoggerMessage.Define<string, string>(LogLevel.Debug, 0, "HtmlPdfSrvPlus({source}) : {message}");
    }
}
