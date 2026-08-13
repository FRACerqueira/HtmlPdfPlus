// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

namespace HtmlPdfPlus
{
    /// <summary>
    /// Explicit declaration of how <see cref="RequestHtmlPdf{T}.Html"/> must be interpreted by
    /// the server, replacing the previous heuristic (<c>Uri.IsWellFormedUriString</c>) that
    /// inferred it from the string's shape.
    /// </summary>
    public enum RenderMode
    {
        /// <summary>
        /// <see cref="RequestHtmlPdf{T}.Html"/> is literal HTML markup to render.
        /// </summary>
        Html = 0,

        /// <summary>
        /// <see cref="RequestHtmlPdf{T}.Html"/> is a URL for the server to navigate to and
        /// capture, subject to the host's configured URL allow-policy before navigation.
        /// </summary>
        Url = 1
    }
}
