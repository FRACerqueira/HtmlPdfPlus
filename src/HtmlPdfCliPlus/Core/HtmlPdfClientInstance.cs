// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using HtmlPdfPlus;
using HtmlPdfShrPlus.Core;
using Microsoft.Extensions.Logging;
using NUglify;
using System.Net.Mime;

namespace HtmlPdfCliPlus.Core
{
    /// <summary>
    /// Represents an instance of the HTML to PDF client.
    /// </summary>
    internal sealed class HtmlPdfClientInstance(string sourcealias, DisableOptionsHtmlToPdf disableOptions) : IHtmlPdfClient
    {
        private ILogger? _logger = null;
        private LogLevel _logLevel = LogLevel.Debug;
        private PdfPageConfig _pdfPageConfig = new();
        private string _html = string.Empty;
        private int _timeout = 30000;
        private bool _htmlparse = false;
        private string? _errorparse = null;
        private Action<string>? _parseError = null;
        private static readonly JsonSerializerOptions jsonoptions = new() { PropertyNameCaseInsensitive = true };

        /// <inheritdoc />
        public IHtmlPdfClient PageConfig(Action<IPdfPageConfig> config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config), "config is null");
            }
            var cfg = new HtmlPdfConfig();
            config.Invoke(cfg);
            if (!disableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableMinifyHtml))
            {
                if (!string.IsNullOrEmpty(cfg.PageConfig.Header))
                {
                    cfg.PageConfig.Header = Uglify.Html(cfg.PageConfig.Header).Code;
                }
                if (!string.IsNullOrEmpty(cfg.PageConfig.Footer))
                {
                     cfg.PageConfig.Footer = Uglify.Html(cfg.PageConfig.Footer).Code;
                }
            }
            _pdfPageConfig = cfg.PageConfig;
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfClient FromHtml(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value), "value is null or empty");
            }
            if (disableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableMinifyHtml))
            {
                _html = value;
                _errorparse = null;
            }
            else
            {
                var minify = Uglify.Html(value);
                _errorparse = minify.Errors.ToString();
                _html = minify.Code;
            }
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfClient FromRazor<T>(string template, T razordata)
        {
            if (string.IsNullOrEmpty(template))
            {
                throw new ArgumentNullException(nameof(template), "template is null or empty");
            }
            var aux = RazorHelpper.CompileTemplate(template, razordata);
            if (disableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableMinifyHtml))
            {
                _html = aux;
            }
            else
            {
                var minify = Uglify.Html(aux);
                _errorparse = minify.Errors.ToString();
                _html = minify.Code;
            }
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfClient Logger(ILogger? value, LogLevel logLevel = LogLevel.Debug)
        {
            if (disableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableLogging))
            {
                return this;
            }
            if (logLevel is LogLevel.Critical or LogLevel.Error or LogLevel.Warning)
            {
                throw new ArgumentException($"Invalid log level {logLevel}");
            }
            _logger = value;
            _logLevel = logLevel;
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfClient Timeout(int value)
        {
            if (value < 1)
            {
                throw new ArgumentException("Timeout must be greater than zero");
            }
            _timeout = value;
            return this;
        }


        /// <inheritdoc />
        public IHtmlPdfClient HtmlParser(bool validate, Action<string> whenhaserror)
        {
            _htmlparse = validate;
            _parseError = whenhaserror;
            return this;
        }


        /// <inheritdoc />
        public async Task<HtmlPdfResult<byte[]>> Run(Func<string, CancellationToken, Task<HtmlPdfResult<byte[]>>> submitHtmlToPdf, CancellationToken token = default)
        {
            return await Run<object, byte[]>(submitHtmlToPdf, null, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<HtmlPdfResult<Tout>> Run<Tin, Tout>(Func<string, CancellationToken, Task<HtmlPdfResult<Tout>>> submitHtmlToPdf, Tin? inputparam, CancellationToken token = default)
        {
            if (_html.Length == 0)
            {
                throw new InvalidOperationException("Html source not found");
            }
            if (submitHtmlToPdf is null)
            {
                throw new ArgumentNullException(nameof(submitHtmlToPdf), "Function for submit is null");
            }
            return await SubmitAsync<Tin, Tout>(submitHtmlToPdf, inputparam, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<HtmlPdfResult<byte[]>> Run(HttpClient httpclient, CancellationToken token = default)
        {
            return Run(httpclient, null, token);
        }

        /// <inheritdoc />
        public async Task<HtmlPdfResult<byte[]>> Run(HttpClient httpclient, string? endpoint, CancellationToken token = default)
        {
            return await Run<object, byte[]>(httpclient, endpoint, null, token);
        }

        /// <inheritdoc />
        public Task<HtmlPdfResult<Tout>> Run<Tin, Tout>(HttpClient httpclient, Tin? customdata, CancellationToken token = default)
        {
            return Run<Tin, Tout>(httpclient, null, customdata, token);
        }

        /// <inheritdoc />
        public async Task<HtmlPdfResult<Tout>> Run<Tin, Tout>(HttpClient httpclient, string? endpoint, Tin? customdata, CancellationToken token = default)
        {
            if (_html.Length == 0)
            {
                throw new InvalidOperationException("Html source not found");
            }
            if (_htmlparse && !string.IsNullOrEmpty(_errorparse) && _parseError is not null)
            { 
                _parseError.Invoke(_errorparse);
            }
            var sw = Stopwatch.StartNew();
            HttpContent content = CreateHttpContent(customdata);
            content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
            try
            {
                var result = await httpclient.PostAsync(endpoint, content, token).ConfigureAwait(false);
                return await HandleHttpResponse<Tout>(result, sw, token).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ex);
            }
            catch (TaskCanceledException ex)
            {
                return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ex);
            }
        }

        /// <summary>
        /// Submits the HTML to the server for conversion to PDF.
        /// </summary>
        /// <typeparam name="Tin">The type of the input parameter.</typeparam>
        /// <typeparam name="Tout">The type of the output result.</typeparam>
        /// <param name="submitHtmlToPdf">The function to submit HTML to PDF conversion.</param>
        /// <param name="inputparam">The input parameter.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The result of the HTML to PDF conversion.</returns>
        private async Task<HtmlPdfResult<Tout>> SubmitAsync<Tin, Tout>(Func<string, CancellationToken, Task<HtmlPdfResult<Tout>>> submitHtmlToPdf, Tin? inputparam, CancellationToken token)
        {
            if (_html.Length == 0)
            {
                throw new InvalidOperationException("Html source not found");
            }
            if (_htmlparse && !string.IsNullOrEmpty(_errorparse) && _parseError is not null)
            {
                _parseError.Invoke(_errorparse);
            }
            var sw = Stopwatch.StartNew();
            LogMessage($"Start Submit at {DateTime.UtcNow}");
            HtmlPdfResult<Tout>? result = null;
            using (var cts = new CancellationTokenSource())
            {
                using var linkcts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, token);
                try
                {
                    string? requestsend = CreateRequestSend(inputparam);
                    cts.CancelAfter(_timeout);
                    var tasksubmit = Task.Run(async () => result = await submitHtmlToPdf(requestsend, linkcts.Token).ConfigureAwait(false), linkcts.Token);

                    var completed = await Task.WhenAny(tasksubmit, Task.Delay(_timeout, linkcts.Token)).ConfigureAwait(false);
                    if (completed != tasksubmit)
                    {
                        result = new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new TimeoutException($"Canceled by Timeout({_timeout})"));
                    }
                    else if (tasksubmit.IsFaulted)
                    {
                        result = new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, tasksubmit.Exception?.InnerException);
                    }
                }
                catch (OperationCanceledException oex)
                {
                    result = HandleOperationCanceledException<Tout>(oex, sw);
                }
                catch (Exception ex)
                {
                    result = new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ex);
                }
                finally
                {
                    cts.Cancel();
                }
            }
            sw.Stop();
            LogMessage($"End Submit at {DateTime.UtcNow} with Elapsed time {sw.Elapsed}. Success {result!.IsSuccess}, Error : {result!.Error?.ToString()} ");
            if (typeof(Tout) == typeof(byte[]))
            {
                if (disableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress) || result.OutputData is null)
                {
                    return result;
                }
                else
                {
                    return new HtmlPdfResult<Tout>(result.IsSuccess, result.BufferDrained, result.ElapsedTime, (Tout?)Convert.ChangeType(result.DecompressBytes(), typeof(Tout)), result.Error);
                }
            }
            return result;
        }

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogMessage(string message)
        {
            if (_logger is null || !_logger.IsEnabled(_logLevel)) return;

            _logger.Log(_logLevel, 0, $"HtmlPdfCliPlus({sourcealias}) : {message}", null, (s, e) => s);
        }

        /// <summary>
        /// Creates the HTTP content for the request.
        /// </summary>
        /// <typeparam name="T">The type of the custom data.</typeparam>
        /// <param name="customdata">The custom data.</param>
        /// <returns>The HTTP content.</returns>
        private StringContent CreateHttpContent<T>(T? customdata)
        {
            return disableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress)
                ? new StringContent(new RequestHtmlPdf<T>(_html, sourcealias, _pdfPageConfig, _timeout, customdata).ToString())
                : new StringContent(JsonSerializer.Serialize(
                    new RequestHtmlPdf<T>(_html, sourcealias, _pdfPageConfig, _timeout, customdata).ToStringCompress()));
        }

        /// <summary>
        /// Creates the request send string.
        /// </summary>
        /// <typeparam name="T">The type of the input parameter.</typeparam>
        /// <param name="inputparam">The input parameter.</param>
        /// <returns>The request send string.</returns>
        private string CreateRequestSend<T>(T? inputparam)
        {
            return disableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress)
                ? JsonSerializer.Serialize(new RequestHtmlPdf<T>(_html, sourcealias, _pdfPageConfig, _timeout, inputparam), jsonoptions)
                : GZipHelper.CompressRequest(sourcealias, _pdfPageConfig, _html, _timeout, inputparam);
        }

        /// <summary>
        /// Handles the operation canceled exception.
        /// </summary>
        /// <typeparam name="Tout">The type of the output result.</typeparam>
        /// <param name="oex">The operation canceled exception.</param>
        /// <param name="sw">The stopwatch.</param>
        /// <returns>The result of the HTML to PDF conversion.</returns>
        private HtmlPdfResult<Tout> HandleOperationCanceledException<Tout>(OperationCanceledException oex, Stopwatch sw)
        {
            if (oex.CancellationToken.IsCancellationRequested)
            {
                LogMessage($"Canceled by Timeout({_timeout})");
                return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new TimeoutException($"Canceled by Timeout({_timeout})", oex));
            }
            else
            {
                LogMessage("Canceled by client");
                return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, oex);
            }
        }

        /// <summary>
        /// Handles the HTTP response.
        /// </summary>
        /// <typeparam name="Tout">The type of the output result.</typeparam>
        /// <param name="result">The HTTP response message.</param>
        /// <param name="sw">The stopwatch.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The result of the HTML to PDF conversion.</returns>
        private async Task<HtmlPdfResult<Tout>> HandleHttpResponse<Tout>(HttpResponseMessage result, Stopwatch sw, CancellationToken token)
        {
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
#if NETSTANDARD2_1
                var resultconvert = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
#if NET8_0_OR_GREATER
                    var resultconvert = await result.Content.ReadAsStringAsync(token).ConfigureAwait(false);
#endif
                if (typeof(Tout) == typeof(byte[]))
                {
                    if (disableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress))
                    {
                        return JsonSerializer.Deserialize<HtmlPdfResult<Tout>>(resultconvert, jsonoptions)!;
                    }
                    else
                    {
                        var auxresult = JsonSerializer.Deserialize<HtmlPdfResult<Tout>>(resultconvert, jsonoptions)!;
                        if (auxresult.OutputData is null)
                        {
                            return auxresult;
                        }
                        return new HtmlPdfResult<Tout>(auxresult.IsSuccess, auxresult.BufferDrained, auxresult.ElapsedTime, (Tout?)Convert.ChangeType(auxresult.DecompressBytes(), typeof(Tout)), auxresult.Error);
                    }
                }
                else
                {
                    return JsonSerializer.Deserialize<HtmlPdfResult<Tout>>(resultconvert, jsonoptions)!;
                }
            }
            else
            {
                return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new HttpRequestException($"{result.StatusCode} : {result.ReasonPhrase}"));
            }
        }
    }
}
