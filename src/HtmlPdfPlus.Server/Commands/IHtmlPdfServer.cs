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
    public interface IHtmlPdfServer<TIn, TOut> : IDisposable
    {
        /// <summary>
        /// Transfer context for <see cref="IHtmlPdfServerContext{TIn, TOut}"/> server context  with input data,and custom actions.
        /// <param name="inputparam">Input data, for customizing HTML before converting to PDF on the server.</param>
        /// </summary>
        /// <returns>An instance of <see cref="IHtmlPdfServerContext{TIn, TOut}"/>.</returns>
        IHtmlPdfServerContext<TIn, TOut> Source(TIn? inputparam = default);


        /// <summary>
        /// Transfer request client for <see cref="IHtmlPdfServerContext{TIn, TOut}"/> server context for custom actions 
        /// </summary>
        /// <param name="requestClient">The compressed data from the request HtmlPdfCliPlus client.</param>
        /// <returns>An instance of <see cref="IHtmlPdfServerContext{TIn, TOut}"/>.</returns>
        IHtmlPdfServerContext<TIn, TOut> Request(string requestClient);

        /// <summary>
        /// Perform HTML to PDF conversion from the request HtmlPdfCliPlus client.
        /// </summary>
        /// <param name="requestClient">The compressed data from the request HtmlPdfCliPlus client.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to perform the conversion.</param>
        /// <returns>An instance of <see cref="HtmlPdfResult{TOut}"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <typeparamref name="TOut"/> is invalid.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="requestClient"/> is invalid.</exception>
        Task<HtmlPdfResult<TOut>> Run(string requestClient, CancellationToken token = default);
    }
}
