// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using Microsoft.Extensions.Logging;
using HtmlPdfPlus.Client.Core;

namespace HtmlPdfPlus
{
    /// <summary>
    /// Fluent interface commands to <see cref="HtmlPdfClientInstance"/>.
    /// </summary>
    public interface IHtmlPdfClient
    {
        /// <summary>
        /// Set PDF page configuration.
        /// </summary>
        /// <param name="config">An action that takes an <see cref="IPdfPageConfig"/> object to configure the page settings.</param>
        /// <returns><see cref="IHtmlPdfClient"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the config action is null.</exception>
        IHtmlPdfClient PageConfig(Action<IPdfPageConfig> config);

        /// <summary>
        /// Set timeout for conversion (default 30000ms).
        /// </summary>
        /// <param name="value">Timeout for conversion in milliseconds.</param>
        /// <returns><see cref="IHtmlPdfClient"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the timeout value is invalid.</exception>
        IHtmlPdfClient Timeout(int value);

        /// <summary>
        /// Set Logger integration.
        /// </summary>
        /// <param name="logger"><see cref="ILogger"/> instance.</param>
        /// <param name="logLevel">Log level, valid levels are: None, Trace, Debug (default), Info. <see cref="LogLevel"/>.</param>
        /// <returns><see cref="IHtmlPdfClient"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the loglevelis invalid.</exception>
        /// 
        IHtmlPdfClient Logger(ILogger? logger, LogLevel logLevel = LogLevel.Debug);

        /// <summary>
        /// Register HTML to be executed by the server.
        /// </summary>
        /// <param name="html">The HTML content.</param>
        /// <returns><see cref="IHtmlPdfClient"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the HTML content is null.</exception>
        IHtmlPdfClient FromHtml(string html);

        /// <summary>
        /// Register Page Url to be executed by the server.
        /// </summary>
        /// <param name="value">The url</param>
        /// <returns><see cref="IHtmlPdfClient"/> instance.</returns>
        IHtmlPdfClient FromUrl(Uri value);

        /// <summary>
        /// Execute the Razor HTML template with the data and register the HTML.
        /// </summary>
        /// <param name="template">Razor template source.</param>
        /// <param name="model">Data to apply to the template.</param>
        /// <typeparam name="T">Type of data model.</typeparam>
        /// <returns><see cref="IHtmlPdfClient"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the template or model is null.</exception>
        IHtmlPdfClient FromRazor<T>(string template, T model);

        /// <summary>
        /// Execute parse validation of the HTML before sending it to the server.
        /// </summary>
        /// <param name="validate">Execute validation.Default <c>false</c></param>
        /// <param name="whenhaserror">Action when has errror. The action input is VisualStudio string format error message </param>
        /// <returns><see cref="IHtmlPdfClient"/> instance.</returns>
        IHtmlPdfClient HtmlParser(bool validate, Action<string> whenhaserror);

        /// <summary>
        /// Submit the HTML to convert to PDF in byte[] by the SubmitHtmlToPdf function.
        /// </summary>
        /// <param name="submitHtmlToPdf">Handler to function submit to server.
        /// <para>A function that takes a request client parameter and a <see cref="CancellationToken"/>, and </para>
        /// <para>returns a <see cref="HtmlPdfResult{T}"/> representing the asynchronous operation of converting HTML to PDF.</para>
        /// </param>
        /// <param name="token"><see cref="CancellationToken"/> token.</param>
        /// <returns>Returns bytes[] from <see cref="HtmlPdfResult{T}"/> representing the asynchronous operation of converting HTML to PDF.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the empty Html source.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the submitHtmlToPdf function is null.</exception>
        Task<HtmlPdfResult<byte[]>> Run(Func<byte[], CancellationToken, Task<HtmlPdfResult<byte[]>>> submitHtmlToPdf, CancellationToken token = default);

        /// <summary>
        /// Submit the HTML to convert to PDF in byte[] via POST <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="httpClient">Instance of <see cref="HttpClient"/>.</param>
        /// <param name="token"><see cref="CancellationToken"/> token.</param>
        /// <returns>Returns bytes[] from <see cref="HtmlPdfResult{T}"/> representing the asynchronous operation of converting HTML to PDF.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the empty Html source.</exception>
        Task<HtmlPdfResult<byte[]>> Run(HttpClient httpClient, CancellationToken token = default);

        /// <summary>
        /// Submit the HTML to convert to PDF in byte[] via POST <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="httpClient">Instance of <see cref="HttpClient"/>.</param>
        /// <param name="endpoint">The endpoint for the HTTP client.</param>
        /// <param name="token"><see cref="CancellationToken"/> token.</param>
        /// <returns>Returns bytes[] from <see cref="HtmlPdfResult{T}"/> representing the asynchronous operation of converting HTML to PDF.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the empty Html source.</exception>
        Task<HtmlPdfResult<byte[]>> Run(HttpClient httpClient, string endpoint, CancellationToken token = default);

        /// <summary>
        /// Submit the HTML to convert to PDF in custom output via the SubmitHtmlToPdf function.
        /// </summary>
        /// <typeparam name="Tin">Type of input data.</typeparam>
        /// <typeparam name="Tout">Type of output data.</typeparam>
        /// <param name="submitHtmlToPdf">Handler to function submit to server.
        /// <para>A function that takes a request client parameter and a <see cref="CancellationToken"/>, and </para>
        /// <para>returns a <see cref="HtmlPdfResult{Tout}"/> representing the asynchronous operation of converting HTML to PDF.</para>
        /// </param>
        /// <param name="customData">Input data, for customizing HTML before converting to PDF on the server.</param>
        /// <param name="token"><see cref="CancellationToken"/> token.</param>
        /// <returns>Returns <see cref="HtmlPdfResult{Tout}"/> representing the asynchronous operation of converting HTML to PDF.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the empty Html source.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the submitHtmlToPdf function or customData is null.</exception>
        Task<HtmlPdfResult<Tout>> Run<Tin, Tout>(Func<byte[], CancellationToken, Task<HtmlPdfResult<Tout>>> submitHtmlToPdf, Tin? customData, CancellationToken token = default);

        /// <summary>
        /// Submit the HTML to convert to PDF in custom output via POST <see cref="HttpClient"/>.
        /// </summary>
        /// <typeparam name="Tin">Type of input data.</typeparam>
        /// <typeparam name="Tout">Type of output data.</typeparam>
        /// <param name="httpClient">Instance of <see cref="HttpClient"/>.</param>
        /// <param name="customData">Input data, for customizing HTML before converting to PDF on the server.</param>
        /// <param name="token"><see cref="CancellationToken"/> token.</param>
        /// <returns>Returns <see cref="HtmlPdfResult{Tout}"/> representing the asynchronous operation of converting HTML to PDF.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the empty Html source.</exception>
        Task<HtmlPdfResult<Tout>> Run<Tin, Tout>(HttpClient httpClient, Tin? customData, CancellationToken token = default);

        /// <summary>
        /// Submit the HTML to convert to PDF in custom output via POST <see cref="HttpClient"/>.
        /// </summary>
        /// <typeparam name="Tin">Type of input data.</typeparam>
        /// <typeparam name="Tout">Type of output data.</typeparam>
        /// <param name="httpClient">Instance of <see cref="HttpClient"/>.</param>
        /// <param name="endpoint">The endpoint for the HTTP client.</param>
        /// <param name="customData">Input data, for customizing HTML before converting to PDF on the server.</param>
        /// <param name="token"><see cref="CancellationToken"/> token.</param>
        /// <returns>Returns <see cref="HtmlPdfResult{Tout}"/> representing the asynchronous operation of converting HTML to PDF.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the empty Html source.</exception>
        Task<HtmlPdfResult<Tout>> Run<Tin, Tout>(HttpClient httpClient, string endpoint, Tin? customData, CancellationToken token = default);
    }
}
