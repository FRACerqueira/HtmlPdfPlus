// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

namespace HtmlPdfPlus
{
    /// <summary>
    /// Fluent interface commands to perform HTML to PDF conversion.
    /// </summary>
    /// <typeparam name="TIn">Type of input data.</typeparam>
    /// <typeparam name="TOut">Type of output data.</typeparam>
    public interface IHtmlPdfServer<TIn, TOut>
    {
        /// <summary>
        /// Function to enrich HTML before performing HTML to PDF conversion.
        /// </summary>
        /// <param name="inputParam">A function that takes a HTML request client, input data of type <typeparamref name="TIn"/>, and a <see cref="CancellationToken"/>, and returns enriched HTML as a string.</param>
        /// <returns>An instance of <see cref="IHtmlPdfServer{TIn, TOut}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="inputParam"/> is null.</exception>
        IHtmlPdfServer<TIn, TOut> BeforePDF(Func<string, TIn?, CancellationToken, Task<string>> inputParam);

        /// <summary>
        /// Function to transform to a new output type after performing HTML to PDF conversion.
        /// </summary>
        /// <param name="outputParam">A function that takes a PDF in byte[], input data of type <typeparamref name="TIn"/>, and a <see cref="CancellationToken"/>, and returns the new output type <typeparamref name="TOut"/>.</param>
        /// <returns>An instance of <see cref="IHtmlPdfServer{TIn, TOut}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="outputParam"/> is null.</exception>
        IHtmlPdfServer<TIn, TOut> AfterPDF(Func<byte[]?, TIn?, CancellationToken, Task<TOut>> outputParam);

        /// <summary>
        /// Perform HTML to PDF conversion.
        /// </summary>
        /// <param name="requestClient">The compressed data from the request HtmlPdfCliPlus client.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to perform the conversion.</param>
        /// <returns>An instance of <see cref="HtmlPdfResult{TOut}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestClient"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="requestClient"/> is invalid.</exception>
        Task<HtmlPdfResult<TOut>> Run(string requestClient, CancellationToken token = default);
    }
}
