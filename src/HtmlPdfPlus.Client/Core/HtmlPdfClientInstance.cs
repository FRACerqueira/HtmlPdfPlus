// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NUglify;
using System.Net.Mime;
using HtmlPdfPlus.Shared.Core;

namespace HtmlPdfPlus.Client.Core
{
    /// <summary>
    /// Represents an instance of the HTML to PDF client.
    /// </summary>
    internal sealed class HtmlPdfClientInstance(string sourcealias, DisableOptionsHtmlToPdf disableOptions) : IHtmlPdfClient
    {
        private ILogger? _logger;
        private LogLevel _logLevel = LogLevel.Debug;
        private PdfPageConfig _pdfPageConfig = new();
        private string _html = string.Empty;
        private RenderMode _mode = RenderMode.Html;
        private int _timeout = 30000;
        private bool _htmlparse;
        private string? _errorparse;
        private Action<string>? _parseError;

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
            _errorparse = null;
            if (disableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableMinifyHtml))
            {
                _html = value;
            }
            else
            {
                var minify = Uglify.Html(value);
                if (minify.HasErrors)
                {
                    _errorparse = string.Join(Environment.NewLine, minify.Errors.Select(e => e.Message));
                }
                _html = minify.Code;
            }
            _mode = RenderMode.Html;
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfClient FromUrl(Uri value)
        {
            _html = value.ToString();
            _mode = RenderMode.Url;
            _errorparse = null;
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
            _errorparse = null;
            if (disableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableMinifyHtml))
            {
                _html = aux;
            }
            else
            {
                var minify = Uglify.Html(aux);
                if (minify.HasErrors)
                {
                    _errorparse = string.Join(Environment.NewLine, minify.Errors.Select(e => e.Message));
                }
                _html = minify.Code;
            }
            _mode = RenderMode.Html;
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
        public async Task<HtmlPdfResult<byte[]>> Run(Func<byte[], CancellationToken, Task<HtmlPdfResult<byte[]>>> submitHtmlToPdf, CancellationToken token = default)
        {
            return await Run<object, byte[]>(submitHtmlToPdf, null, token);
        }

        /// <inheritdoc />
        public async Task<HtmlPdfResult<Tout>> Run<Tin, Tout>(Func<byte[], CancellationToken, Task<HtmlPdfResult<Tout>>> submitHtmlToPdf, Tin? inputparam, CancellationToken token = default)
        {
            if (_html.Length == 0)
            {
                throw new InvalidOperationException("Html source not found");
            }
            if (submitHtmlToPdf is null)
            {
                throw new ArgumentNullException(nameof(submitHtmlToPdf), "Function for submit is null");
            }
            return await SubmitAsync<Tin, Tout>(submitHtmlToPdf, inputparam, token);
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
            HttpContent content = await CreateHttpContent(customdata);
            content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
            // .Timeout() only travels inside the request body for the server to honor; without
            // a local deadline here, this call relied entirely on HttpClient.Timeout (100s by
            // default) or the caller's own token, so a slow/unresponsive server ignored the
            // timeout configured via the fluent API.
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(_timeout);
            try
            {
                var result = await httpclient.PostAsync(endpoint, content, cts.Token);
                return await HandleHttpResponse<Tout>(result, sw, cts.Token);
            }
            catch (HttpRequestException ex)
            {
                return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ErrorInfo.FromException(ex));
            }
            catch (TaskCanceledException ex)
            {
                if (cts.IsCancellationRequested && !token.IsCancellationRequested)
                {
                    return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new ErrorInfo(ErrorCode.Timeout, $"Canceled by Timeout({_timeout})", retryable: true));
                }
                return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ErrorInfo.FromException(ex));
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
        private async Task<HtmlPdfResult<Tout>> SubmitAsync<Tin, Tout>(Func<byte[], CancellationToken, Task<HtmlPdfResult<Tout>>> submitHtmlToPdf, Tin? inputparam, CancellationToken token)
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
                    byte[] requestsend = await CreateRequestSend(inputparam);
                    cts.CancelAfter(_timeout);
                    var tasksubmit = Task.Run(async () => result = await submitHtmlToPdf(requestsend, linkcts.Token).ConfigureAwait(false), linkcts.Token);

                    // Independent wall-clock backstop: driven only by elapsed time and the
                    // caller's own token, never by cts/linkcts. tasksubmit is ALSO driven by
                    // linkcts, so racing it against a delay built from the same token made both
                    // sides resolve off the same cancellation event - when tasksubmit "won" in
                    // the Canceled state, IsFaulted (below) missed it and left `result` null.
                    // This backstop still guarantees Run() returns within _timeout even when
                    // submitHtmlToPdf never observes the CancellationToken it was given.
                    var backstop = Task.Delay(_timeout, token);
                    var completed = await Task.WhenAny(tasksubmit, backstop);
                    if (completed == backstop)
                    {
                        result = token.IsCancellationRequested
                            ? new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ErrorInfo.FromException(new OperationCanceledException("Canceled by client", token)))
                            : new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new ErrorInfo(ErrorCode.Timeout, $"Canceled by Timeout({_timeout})", retryable: true));
                    }
                    else
                    {
                        // tasksubmit finished first: observe it so a fault or a cancellation
                        // raised by the delegate itself surfaces as a real exception below via
                        // the catch blocks (which already build ErrorInfo correctly), instead
                        // of being inferred (and possibly missed, e.g. a Canceled task has a
                        // null Exception) from Task state directly.
                        await tasksubmit;
                    }
                }
                catch (OperationCanceledException oex)
                {
                    result = HandleOperationCanceledException<Tout>(oex, sw);
                }
                catch (Exception ex)
                {
                    result = new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ErrorInfo.FromException(ex));
                }
                finally
                {
                    cts.Cancel();
                }
            }
            sw.Stop();
            LogMessage($"End Submit at {DateTime.UtcNow} with Elapsed time {sw.Elapsed}. Success {result!.IsSuccess}, Error : { result!.Error } ");
            if (typeof(Tout) == typeof(byte[]))
            {
                if (disableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress) || result.OutputData is null)
                {
                    return result;
                }
                else
                {
                    return new HtmlPdfResult<Tout>(result.IsSuccess, result.BufferDrained, result.ElapsedTime, (Tout)(object)result.OutputData, result.Error);
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
        /// <returns>The HTTP <see cref="ByteArrayContent"/>.</returns>
        private async Task<StringContent> CreateHttpContent<T>(T? customdata)
        {
            // Stamped as close to the actual send as possible, so a receiving server can
            // subtract real transit time from Timeout instead of restarting the deadline
            // fresh on arrival (see RequestHtmlPdf.SentAtUtc).
            var sentAtUtc = DateTimeOffset.UtcNow;
            return disableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress)
                ? new StringContent(JsonSerializer.Serialize(new RequestHtmlPdf<T>(_html, sourcealias, _pdfPageConfig, _timeout, customdata, _mode, sentAtUtc).ToBytes()))
                : new StringContent(JsonSerializer.Serialize(await new RequestHtmlPdf<T>(_html, sourcealias, _pdfPageConfig, _timeout, customdata, _mode, sentAtUtc).ToBytesCompress()));
        }

        /// <summary>
        /// Creates the request send string.
        /// </summary>
        /// <typeparam name="T">The type of the input parameter.</typeparam>
        /// <param name="inputparam">The input parameter.</param>
        /// <returns>The request send in byte[].</returns>
        private async Task<byte[]> CreateRequestSend<T>(T? inputparam)
        {
            // Stamped as close to the actual send as possible, so a receiving server can
            // subtract real transit time from Timeout instead of restarting the deadline
            // fresh on arrival (see RequestHtmlPdf.SentAtUtc).
            var sentAtUtc = DateTimeOffset.UtcNow;
            return disableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress)
                ? new RequestHtmlPdf<T>(_html, sourcealias, _pdfPageConfig, _timeout, inputparam, _mode, sentAtUtc).ToBytes()
                : await new RequestHtmlPdf<T>(_html, sourcealias, _pdfPageConfig, _timeout, inputparam, _mode, sentAtUtc).ToBytesCompress();
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
                return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, new ErrorInfo(ErrorCode.Timeout, $"Canceled by Timeout({_timeout})", retryable: true));
            }
            else
            {
                LogMessage("Canceled by client");
                return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, ErrorInfo.FromException(oex));
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
        private static async Task<HtmlPdfResult<Tout>> HandleHttpResponse<Tout>(HttpResponseMessage result, Stopwatch sw, CancellationToken token)
        {
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // A byte[] output is the generated PDF itself: the body IS the PDF, served
                // directly (e.g. application/pdf) instead of wrapped in a JSON envelope with the
                // bytes re-encoded as base64 - which only ever added size, since PDFs are already
                // a largely-compressed binary format. Transport-level compression, if any, is the
                // host's Content-Encoding, decoded automatically by HttpClient - not an
                // application-level gzip step here.
                if (typeof(Tout) == typeof(byte[]))
                {
                    var bytes = await result.Content.ReadAsByteArrayAsync(token);
                    return new HtmlPdfResult<Tout>(true, false, sw.Elapsed, (Tout)(object)bytes, null);
                }
                using var resultconvert = await result.Content.ReadAsStreamAsync(token);
                return (await JsonSerializer.DeserializeAsync<HtmlPdfResult<Tout>>(resultconvert, GZipHelper.JsonOptions, token))!;
            }
            // A non-2xx status line carries the failure, not an embedded IsSuccess flag: the body
            // is expected to be the structured ErrorInfo contract. Fall back to a generic error
            // built from the status line if the body doesn't match it (e.g. an upstream proxy
            // error, or a host that hasn't adopted the contract).
            ErrorInfo? error = null;
            try
            {
                using var errorStream = await result.Content.ReadAsStreamAsync(token);
                error = await JsonSerializer.DeserializeAsync<ErrorInfo>(errorStream, GZipHelper.JsonOptions, token);
            }
            catch (JsonException)
            {
                // Body wasn't a valid ErrorInfo - fall through to the generic error below.
            }
            if (error is null)
            {
                // No structured body (e.g. a proxy/load balancer returned the 503 itself, not the
                // app) - the standard Retry-After header may still be present and is the most
                // likely place a real backpressure signal shows up, so it must not be dropped here.
                var retryAfterSeconds = result.Headers.RetryAfter?.Delta is TimeSpan delta
                    ? (int)Math.Ceiling(delta.TotalSeconds)
                    : (int?)null;
                error = new ErrorInfo(
                    ErrorCode.Unknown,
                    $"{result.StatusCode} : {result.ReasonPhrase}",
                    retryable: retryAfterSeconds is not null,
                    retryAfterSeconds: retryAfterSeconds);
            }
            return new HtmlPdfResult<Tout>(false, false, sw.Elapsed, default, error);
        }
    }
}
