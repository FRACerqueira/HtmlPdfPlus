// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************


namespace HtmlPdfPlus
{
    /// <summary>
    /// Fluent interface commands to input sources HTML or Url to PDF conversion.
    /// </summary>
    /// <typeparam name="TIn">Type of input data.</typeparam>
    /// <typeparam name="TOut">Type of output data.</typeparam>
    public interface IHtmlPdfServerContext<TIn,TOut>
    {
        /// <summary>
        /// Register the HTML to be executed by the server in the execution context. This command disables data compression.
        /// </summary>
        /// <param name="html">The HTML content.</param>
        /// <param name="converttimeout">Timeout for conversion in milliseconds</param>
        /// <param name="minify"><c>True</c> to minify html. Default value is <c>true</c></param>
        /// <returns>An instance of <see cref="IHtmlPdfServerContext{TIn,TOut}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the HTML content is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the timeout value is invalid.</exception>
        IHtmlPdfServerContext<TIn, TOut> FromHtml(string html, int converttimeout = 30000, bool minify = true);

        /// <summary>
        /// Register Page Url to be executed by the server in the execution context. This command disables data compression e minify.
        /// </summary>
        /// <param name="value">The url</param>
        /// <param name="converttimeout">Timeout for conversion in milliseconds</param>
        /// <returns>An instance of <see cref="IHtmlPdfServerContext{TIn,TOut}"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the timeout value is invalid.</exception>
        IHtmlPdfServerContext<TIn, TOut> FromUrl(Uri value, int converttimeout = 30000);

        /// <summary>
        /// Execute the Razor HTML template with the data and register the HTML by the server  to be executed by the server in the execution context.This command disables data compression.
        /// </summary>
        /// <param name="template">Razor template source.</param>
        /// <param name="model">Data to apply to the template.</param>
        /// <param name="converttimeout">Timeout for conversion in milliseconds</param>
        /// <param name="minify"><c>True</c> to minify html. Default value is <c>true</c></param>
        /// <typeparam name="T">Type of data model.</typeparam>
        /// <returns>An instance of <see cref="IHtmlPdfServerContext{TIn,TOut}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the template or model is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the timeout value is invalid.</exception>
        IHtmlPdfServerContext<TIn,TOut> FromRazor<T>(string template, T model, int converttimeout = 30000, bool minify = true);

        /// <summary>
        /// Function to enrich HTML or Url before performing conversion.
        /// </summary>
        /// <param name="inputParam">A function that takes a HTML or url, input data of type <typeparamref name="TIn"/>, and a <see cref="CancellationToken"/>, and returns enriched HTML or url as a string.</param>
        /// <returns>An instance of <see cref="IHtmlPdfServer{TIn, TOut}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="inputParam"/> is null.</exception>
        IHtmlPdfServerContext<TIn, TOut> BeforePDF(Func<string, TIn?, CancellationToken, Task<string>> inputParam);

        /// <summary>
        /// Function to transform to a new output type after performing HTML to PDF conversion.
        /// </summary>
        /// <param name="outputParam">A function that takes a PDF in byte[], input data of type <typeparamref name="TIn"/>, and a <see cref="CancellationToken"/>, and returns the new output type <typeparamref name="TOut"/>.</param>
        /// <returns>An instance of <see cref="IHtmlPdfServer{TIn, TOut}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="outputParam"/> is null.</exception>
        IHtmlPdfServerContext<TIn, TOut> AfterPDF(Func<byte[]?, TIn?, CancellationToken, Task<TOut>> outputParam);

        /// <summary>
        /// Perform HTML to PDF conversion from context data sources
        /// </summary>
        /// <param name="token">The <see cref="CancellationToken"/> to perform the conversion.</param>
        /// <returns>An instance of <see cref="HtmlPdfResult{TOut}"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the empty source Html or Url.</exception>
        /// <exception cref="ArgumentException">Thrown when <typeparamref name="TOut"/> is invalid.</exception>
        /// <exception cref="ArgumentException">Thrown when <see cref="IHtmlPdfServer{TIn, TOut}.ScopeRequest(byte[])"/> is invalid.</exception>
        Task<HtmlPdfResult<TOut>> Run(CancellationToken token = default);
    }
}
