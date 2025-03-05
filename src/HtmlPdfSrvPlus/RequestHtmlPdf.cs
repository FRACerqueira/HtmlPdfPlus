// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfShrPlus.Core;
using NUglify;

namespace HtmlPdfPlus
{
    /// <summary>
    /// Request data to convert Html to PDF using only HtmlPdfPlus Server
    /// </summary>
    public static class RequestHtmlPdf
    {
        /// <summary>
        /// Create string representation of Html to be used in Server engine
        /// </summary>
        /// <param name="html">The Html to convert</param>
        /// <param name="converttimeout">Timeout for conversion in milliseconds</param>
        /// <param name="minify"><c>True</c> to minify html. Default value is <c>true</c></param>
        /// <param name="compress"><c>True</c> to compress html in gzip-base64. Default value is <c>false</c></param>
        /// <returns>String representation of Html to be used in Server engine</returns>
        /// <exception cref="ArgumentException"></exception>
        public static string Create(string html, int converttimeout = 30000, bool minify = true, bool compress = false)
        {
            ValidateTimeout(converttimeout);
            var aux = ProcessHtml(html, minify);

            return compress
                ? new RequestHtmlPdf<object>(aux, timeout: converttimeout).ToStringCompress()
                : new RequestHtmlPdf<object>(aux, timeout: converttimeout).ToString();
        }

        /// <summary>
        /// Create string representation of Html to be used in Server engine
        /// </summary>
        /// <param name="html">The Html to convert</param>
        /// <param name="config">An action that takes an <see cref="IPdfPageConfig"/> object to configure the page settings.</param>
        /// <param name="converttimeout">Timeout for conversion in milliseconds</param>
        /// <param name="minify"><c>True</c> to minify html. Default value is <c>true</c></param>
        /// <param name="compress"><c>True</c> to compress html in gzip-base64. Default value is <c>false</c></param>
        /// <returns>String representation of Html to be used in Server engine</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static string Create(string html, Action<IPdfPageConfig> config, int converttimeout = 30000, bool minify = true, bool compress = false)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config), "Config cannot be null");
            }

            ValidateTimeout(converttimeout);
            var aux = ProcessHtml(html, minify);
            var cfg = new HtmlPdfConfig();
            config.Invoke(cfg);

            return compress
                ? new RequestHtmlPdf<object>(aux, config: cfg.PageConfig, timeout: converttimeout).ToStringCompress()
                : new RequestHtmlPdf<object>(aux, config: cfg.PageConfig, timeout: converttimeout).ToString();
        }

        /// <summary>
        /// Create string representation of Html to be used in Server engine
        /// </summary>
        /// <typeparam name="T">Input type</typeparam>
        /// <param name="html">The Html to convert</param>
        /// <param name="config">An action that takes an <see cref="IPdfPageConfig"/> object to configure the page settings.</param>
        /// <param name="converttimeout">Timeout for conversion in milliseconds</param>
        /// <param name="param">The input parameter used in BeforePDF and AfterPDF for custom action at server</param>
        /// <param name="minify"><c>True</c> to minify html. Default value is <c>true</c></param>
        /// <param name="compress"><c>True</c> to compress html in gzip-base64. Default value is <c>false</c></param>
        /// <returns>String representation of Html to be used in Server engine</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static string Create<T>(string html, Action<IPdfPageConfig> config, int converttimeout = 30000, T? param = default, bool minify = true, bool compress = false)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config), "Config cannot be null");
            }

            ValidateTimeout(converttimeout);
            var aux = ProcessHtml(html, minify);
            var cfg = new HtmlPdfConfig();
            config.Invoke(cfg);

            return compress
                ? new RequestHtmlPdf<T>(aux, config: cfg.PageConfig, timeout: converttimeout, inputparam: param).ToStringCompress()
                : new RequestHtmlPdf<T>(aux, config: cfg.PageConfig, timeout: converttimeout, inputparam: param).ToString();
        }

        /// <summary>
        /// Create string representation of Html to be used in Server engine
        /// </summary>
        /// <typeparam name="T">Input type</typeparam>
        /// <param name="html">The Html to convert</param>
        /// <param name="converttimeout">Timeout for conversion in milliseconds</param>
        /// <param name="param">The input parameter used in BeforePDF and AfterPDF for custom action at server</param>
        /// <param name="minify"><c>True</c> to minify html. Default value is <c>true</c></param>
        /// <param name="compress"><c>True</c> to compress html in gzip-base64. Default value is <c>false</c></param>
        /// <returns>String representation of Html to be used in Server engine</returns>
        /// <exception cref="ArgumentException"></exception>
        public static string Create<T>(string html, int converttimeout = 30000, T? param = default, bool minify = true, bool compress = false)
        {
            ValidateTimeout(converttimeout);
            var aux = ProcessHtml(html, minify);

            return compress
                ? new RequestHtmlPdf<T>(aux, timeout: converttimeout, inputparam: param).ToStringCompress()
                : new RequestHtmlPdf<T>(aux, timeout: converttimeout, inputparam: param).ToString();
        }

        private static void ValidateTimeout(int converttimeout)
        {
            if (converttimeout < 1)
            {
                throw new ArgumentException("Convert timeout must be greater than zero");
            }
        }

        private static string ProcessHtml(string html, bool minify)
        {
            var aux = string.IsNullOrEmpty(html) ? string.Empty : html;
            if (minify)
            {
                aux = Uglify.Html(aux).Code;
            }
            return aux;
        }
    }
}
