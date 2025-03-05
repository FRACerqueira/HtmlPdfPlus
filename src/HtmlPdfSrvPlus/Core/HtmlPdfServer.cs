// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using HtmlPdfPlus;
using HtmlPdfShrPlus.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace HtmlPdfSrvPlus.Core
{
    /// <summary>
    /// Represents a server for converting HTML to PDF.
    /// </summary>
    /// <typeparam name="Tin">The type of input data.</typeparam>
    /// <typeparam name="Tout">The type of output data.</typeparam>
    internal sealed class HtmlPdfServer<Tin, Tout> : IHtmlPdfServer<Tin, Tout>, IDisposable
    {
        private Func<string, Tin?, CancellationToken, Task<string>>? _inputparam = null;
        private Func<byte[]?, Tin?, CancellationToken, Task<Tout>>? _outputparam = null;
        private bool isDisposed;
        private readonly HtmlPdfBuilder _pdfSrvBuilder;
        private readonly string _sourcealias;
        private static readonly JsonSerializerOptions jsonoptions = new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlPdfServer{Tin, Tout}"/> class.
        /// </summary>
        /// <param name="pdfSrvBuilder">The PDF service builder.</param>
        /// <param name="sourcealias">The source alias.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pdfSrvBuilder"/> is null.</exception>
#pragma warning disable IDE0290 // Use primary constructor
        public HtmlPdfServer(HtmlPdfBuilder? pdfSrvBuilder, string sourcealias)
        {
            _pdfSrvBuilder = pdfSrvBuilder ?? throw new ArgumentNullException(nameof(pdfSrvBuilder), "The pdfSrvBuilder is null");
            _sourcealias = sourcealias;
        }
#pragma warning restore IDE0290 // Use primary constructor

        /// <inheritdoc />
        public IHtmlPdfServer<Tin, Tout> BeforePDF(Func<string, Tin?, CancellationToken, Task<string>> inputparam)
        {
            _inputparam = inputparam ?? throw new ArgumentNullException(nameof(inputparam), "null reference");
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfServer<Tin, Tout> AfterPDF(Func<byte[]?, Tin?, CancellationToken, Task<Tout>> outputparam)
        {
            _outputparam = outputparam ?? throw new ArgumentNullException(nameof(outputparam), "null reference");
            return this;
        }

        /// <inheritdoc />
        public async Task<HtmlPdfResult<Tout>> Run(string requestclient, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(requestclient))
            {
                throw new ArgumentNullException(nameof(requestclient), "request client is null or empty");
            }
            if (_outputparam is null && typeof(Tout) != typeof(byte[]))
            {
                throw new ArgumentException("The output type when there is no custom output parameter must be byte[]");
            }

            var sw = Stopwatch.StartNew();
            LogMessage($"Start Convert Html to PDF from Server at {DateTime.Now}");
            RequestHtmlPdf<Tin> requestHtmlPdf;
            try
            {
                if (_pdfSrvBuilder.DisableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress))
                {
                    requestHtmlPdf = JsonSerializer.Deserialize<RequestHtmlPdf<Tin>>(requestclient, jsonoptions)!;
                }
                else
                {
                    requestHtmlPdf = GZipHelper.DecompressRequest<Tin>(requestclient);
                    LogMessage($"Decompress Request after {sw.Elapsed}");
                }
                requestHtmlPdf.Config ??= _pdfSrvBuilder.Config;

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

            if (_inputparam is not null)
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(requestHtmlPdf.Timeout);
                using var executeToken = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
                try
                {
                    var taskinput = Task.Run(async () =>
                    {
                        requestHtmlPdf.ChangeHtml(await _inputparam(requestHtmlPdf.Html, requestHtmlPdf.InputParam, executeToken.Token), 
                            _pdfSrvBuilder.DisableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableMinifyHtml));
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
                    bytespdf = await GeneratePDF(requestHtmlPdf, reamaindtime, executeToken.Token);
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

            if (_outputparam is not null)
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
                        var aux = await _outputparam(bytespdf, requestHtmlPdf.InputParam, executeToken.Token);
                        if (typeof(Tout) == typeof(byte[]))
                        {
                            if (_pdfSrvBuilder.DisableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress))
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
            if (_pdfSrvBuilder.DisableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress))
            {
                return new HtmlPdfResult<Tout>(true, false, sw.Elapsed, (Tout?)Convert.ChangeType(bytespdf, typeof(Tout)), null);
            }
            var compresspdf = GZipHelper.Compress(bytespdf);
            return new HtmlPdfResult<Tout>(true, false, sw.Elapsed, (Tout?)Convert.ChangeType(compresspdf, typeof(Tout)), null);
        }

        private async Task<byte[]?> GeneratePDF(RequestHtmlPdf<Tin> request, long remaindtime, CancellationToken token)
        {
            IPage? page = null;
            byte[] resultpdf = [];
            try
            {
                page = _pdfSrvBuilder!.Acquire(token);
                if (page == null)
                {
                    return null;
                }
                await page.SetContentAsync(request.Html, new PageSetContentOptions
                {
                    Timeout = remaindtime,
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });
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
                    await _pdfSrvBuilder!.RestoreAvailableBuffer(page!);
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
            _pdfSrvBuilder?.Dispose();
        }

        private void LogMessage(string message)
        {
            if (_pdfSrvBuilder is null || _pdfSrvBuilder.Log is null || (!_pdfSrvBuilder.Log?.IsEnabled(_pdfSrvBuilder.LevelLog) ?? false)) return;

            switch (_pdfSrvBuilder.LevelLog)
            {
                case LogLevel.None:
                    return;
                case LogLevel.Trace:
                    logMessageForTrc(_pdfSrvBuilder.Log!, _sourcealias, message, null);
                    break;
                case LogLevel.Information:
                    logMessageForInf(_pdfSrvBuilder.Log!, _sourcealias, message, null);
                    break;
                case LogLevel.Debug:
                    logMessageForDbg(_pdfSrvBuilder.Log!, _sourcealias, message, null);
                    break;
            }
        }

        // Reusable logging
        private static readonly Action<ILogger, string, string, Exception?> logMessageForInf = LoggerMessage.Define<string, string>(LogLevel.Information, 0, "HtmlPdfSrvPlus({source}) : {message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForTrc = LoggerMessage.Define<string, string>(LogLevel.Trace, 0, "HtmlPdfSrvPlus({source}) : {message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForDbg = LoggerMessage.Define<string, string>(LogLevel.Debug, 0, "HtmlPdfSrvPlus({source}) : {message}");
    }
}
