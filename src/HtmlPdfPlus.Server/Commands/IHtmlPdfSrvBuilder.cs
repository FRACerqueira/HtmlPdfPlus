// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************


using Microsoft.Extensions.Logging;

namespace HtmlPdfPlus
{
    /// <summary>
    /// Fluent interface commands to set instance of Chromium serverless browser.
    /// </summary>
    public interface IHtmlPdfSrvBuilder
    {
        /// <summary>
        /// Set Initial Arguments for Chromium serverless browser.
        /// </summary>
        /// <param name="args">List of arguments separated by ';'.</param>
        /// <returns><see cref="IHtmlPdfSrvBuilder"/> instance.</returns>
        IHtmlPdfSrvBuilder InitArguments(string? args);

        /// <summary>
        /// Set Initial Arguments for Chromium serverless browser.
        /// </summary>
        /// <param name="args">List of arguments.</param>
        /// <returns><see cref="IHtmlPdfSrvBuilder"/> instance.</returns>
        IHtmlPdfSrvBuilder InitArguments(string[] args);

        /// <summary>
        /// Number of pages available to render HTML and convert to PDF.
        /// </summary>
        /// <param name="buffer">Number of pages available. Default is 5.</param>
        /// <returns><see cref="IHtmlPdfSrvBuilder"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the buffer value is invalid.</exception>
        IHtmlPdfSrvBuilder PagesBuffer(byte buffer = 5);

        /// <summary>
        /// Number of idle milliseconds to retry acquiring an available page.
        /// </summary>
        /// <param name="value">Number of milliseconds. Default is 10. The value must be between 10 and 500.</param>
        /// <returns><see cref="IHtmlPdfSrvBuilder"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the value is out of range.</exception>
        IHtmlPdfSrvBuilder AcquireWaitTime(int value = 10);

        /// <summary>
        /// Options to disable internal features.
        /// </summary>
        /// <param name="options">Options to disable. <see cref="DisableOptionsHtmlToPdf"/>.</param>
        /// <returns><see cref="IHtmlPdfSrvBuilder"/> instance.</returns>
        IHtmlPdfSrvBuilder DisableFeatures(DisableOptionsHtmlToPdf options);

        /// <summary>
        /// Number of milliseconds to timeout acquiring an available page.
        /// </summary>
        /// <param name="value">Number of milliseconds. Default is 5000. The value must be greater than or equal to 10.</param>
        /// <returns><see cref="IHtmlPdfSrvBuilder"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the value is out of range.</exception>
        IHtmlPdfSrvBuilder AcquireTimeout(int value = 5000);

        /// <summary>
        /// The default Config PDF page when not provided.
        /// </summary>
        /// <param name="value">The default value. <see cref="PdfPageConfig"/>.</param>
        /// <returns><see cref="IHtmlPdfSrvBuilder"/> instance.</returns>
        IHtmlPdfSrvBuilder DefaultConfig(PdfPageConfig value);

        /// <summary>
        /// The default Config PDF page when not provided.
        /// </summary>
        /// <param name="config">An action that takes an <see cref="IPdfPageConfig"/> object to configure the page settings.</param>
        /// <returns><see cref="IHtmlPdfSrvBuilder"/> instance.</returns>
        IHtmlPdfSrvBuilder DefaultConfig(Action<IPdfPageConfig> config);

        /// <summary>
        /// Set Logger integration.
        /// </summary>
        /// <param name="logLevel">Log level. The valid levels are: None, Trace, Debug (default), Info. <see cref="LogLevel"/>.</param>
        /// <param name="categoryName">Name of category logger. Default is "HtmlPdfServer".</param>
        /// <returns><see cref="IHtmlPdfSrvBuilder"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the log level or category name is invalid.</exception>
        IHtmlPdfSrvBuilder Logger(LogLevel logLevel, string categoryName = "HtmlPdfServer");
    }
}
