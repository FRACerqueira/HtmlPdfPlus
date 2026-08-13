// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

namespace HtmlPdfPlus
{
    /// <summary>
    /// Stable, language-agnostic classification for a <see cref="HtmlPdfResult{T}"/> failure.
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>
        /// The failure does not fit any other known category.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The request itself was invalid (missing/empty Html, invalid configuration, malformed payload).
        /// </summary>
        InvalidRequest,

        /// <summary>
        /// The operation did not complete within the configured timeout.
        /// </summary>
        Timeout,

        /// <summary>
        /// The operation was canceled by a token other than the timeout (caller/host shutdown, a custom hook or transport).
        /// </summary>
        Canceled,

        /// <summary>
        /// No page was available from the render pool within the configured acquire timeout.
        /// </summary>
        PoolExhausted,

        /// <summary>
        /// The HTML/URL failed to render to PDF.
        /// </summary>
        RenderFailed,

        /// <summary>
        /// An unexpected internal error occurred.
        /// </summary>
        Internal
    }
}
