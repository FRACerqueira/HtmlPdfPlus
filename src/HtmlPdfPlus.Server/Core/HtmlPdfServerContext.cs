// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Diagnostics;
using System.Text.Json;
using HtmlPdfPlus.Shared.Core;
using NUglify;

namespace HtmlPdfPlus.Server.Core
{
    /// <summary>
    /// Represents a server context for converting HTML to PDF.
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="htmlPdfServer">Instance of <see cref="HtmlPdfServer{Tin, Tout}"/>.</param>
    /// <param name="inputparam">Input data, for customizing HTML before converting to PDF on the server.</param>
    /// <param name="requestClient">The compressed data from the request HtmlPdfCliPlus client.</param>
    internal sealed class HtmlPdfServerContext<TIn, TOut>(HtmlPdfServer<TIn, TOut> htmlPdfServer, TIn? inputparam, string? requestClient) : IHtmlPdfServerContext<TIn, TOut>, IDisposable
    {
        private bool isDisposed;
        private Func<string, TIn?, CancellationToken, Task<string>>? _inputparam = null;
        private Func<byte[]?, TIn?, CancellationToken, Task<TOut>>? _outputparam = null;
        private string _html = string.Empty;
        private int _timeout = 30000;

        /// <inheritdoc />
        public IHtmlPdfServerContext<TIn, TOut> BeforePDF(Func<string, TIn?, CancellationToken, Task<string>> inputparam)
        {
            _inputparam = inputparam ?? throw new ArgumentNullException(nameof(inputparam), "null reference");
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfServerContext<TIn, TOut> AfterPDF(Func<byte[]?, TIn?, CancellationToken, Task<TOut>> outputparam)
        {
            _outputparam = outputparam ?? throw new ArgumentNullException(nameof(outputparam), "null reference");
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfServerContext<TIn, TOut> FromHtml(string html, int converttimeout = 30000, bool minify = true)
        {
            if (string.IsNullOrEmpty(html))
            {
                throw new ArgumentNullException(nameof(html), "Html is null or empty");
            }
            if (!minify)
            {
                _html = html;
            }
            else
            {
                _html = Uglify.Html(html).Code;
            }
            _timeout = converttimeout;
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfServerContext<TIn, TOut> FromRazor<T>(string template, T model, int converttimeout = 30000, bool minify = true)
        {
            if (string.IsNullOrEmpty(template))
            {
                throw new ArgumentNullException(nameof(template), "template is null or empty");
            }
            var aux = RazorHelpper.CompileTemplate(template, model);
            if (minify)
            {
                _html = aux;
            }
            else
            {
                _html = Uglify.Html(aux).Code;
            }
            _timeout = converttimeout;
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfServerContext<TIn, TOut> FromUrl(Uri value, int converttimeout = 30000)
        {
            _html = value.ToString();
            _timeout = converttimeout;
            return this;
        }

        /// <inheritdoc />
        public async Task<HtmlPdfResult<TOut>> Run(CancellationToken token = default)
        {
            var sw = Stopwatch.StartNew();
            RequestHtmlPdf<TIn> requestHtmlPdf;
            if (!string.IsNullOrEmpty(requestClient))
            {
                if (htmlPdfServer.PdfSrvBuilder.DisableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress))
                {
                    requestHtmlPdf = JsonSerializer.Deserialize<RequestHtmlPdf<TIn>>(requestClient, GZipHelper.JsonOptions)!;
                }
                else
                {
                    requestHtmlPdf = GZipHelper.DecompressRequest<TIn>(requestClient);
                }
                requestHtmlPdf.Config ??= htmlPdfServer.PdfSrvBuilder.Config;
            }
            else
            {
                requestHtmlPdf = new RequestHtmlPdf<TIn>(_html, htmlPdfServer.SourceAlias, htmlPdfServer.PdfSrvBuilder.Config, _timeout, inputparam);
            }
            try
            {
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
                return new HtmlPdfResult<TOut>(false, false, sw.Elapsed, default, ex);
            }
            var isurl = Uri.IsWellFormedUriString(requestHtmlPdf.Html, UriKind.RelativeOrAbsolute);
            var disabledcompress = false;
            if (!string.IsNullOrEmpty(requestClient))
            {
                disabledcompress = htmlPdfServer.PdfSrvBuilder.DisableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableCompress);
            }
            return await htmlPdfServer.RunServer(isurl, _inputparam, _outputparam, sw, requestHtmlPdf, disabledcompress, token);
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
            htmlPdfServer?.Dispose();
        }
    }
}
