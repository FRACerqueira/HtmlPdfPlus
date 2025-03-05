// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

namespace HtmlPdfPlus
{
    /// <summary>
    /// Options for disable internal features 
    /// </summary>
    [Flags]
    public enum DisableOptionsHtmlToPdf
    {
        /// <summary>
        /// Enable all defaut features
        /// </summary>
        EnabledAllFeatures = 0,
        /// <summary>
        /// Disable minify HTML content
        /// </summary>
        DisableMinifyHtml = 1,
        /// <summary>
        /// Disable Compress data to client and server
        /// <para>
        /// The client and server must be the same to function correctly
        /// </para>
        /// </summary>
        DisableCompress = 2,
        /// <summary>
        /// Disable all log
        /// </summary>
        DisableLogging = 4,
    }
}
