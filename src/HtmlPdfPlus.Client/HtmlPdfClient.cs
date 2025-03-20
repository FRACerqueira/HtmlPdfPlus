// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus.Client.Core;

namespace HtmlPdfPlus
{
    /// <summary>
    /// Fluent interface commands to perform client HTML to PDF conversion 
    /// </summary>
    public static class HtmlPdfClient
    {
        /// <summary>
        /// Options for disabling internal features.
        /// </summary>
        public static DisableOptionsHtmlToPdf DisableOptions { get; set; } = DisableOptionsHtmlToPdf.EnabledAllFeatures;

        /// <summary>
        /// Create an instance of Html to Pdf Client
        /// </summary>
        /// <param name="alias">Alias name for client</param>
        /// <returns><see cref="IHtmlPdfClient"/> instance.</returns>
        public static IHtmlPdfClient Create(string? alias = null)
        {
            return new HtmlPdfClientInstance(alias ?? string.Empty, DisableOptions);
        }
    }
}

