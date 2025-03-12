// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

namespace HtmlPdfPlus
{
    /// <summary>
    /// The Config PDF page.
    /// </summary>
    public sealed class PdfPageConfig
    {
        /// <summary>
        /// Create instance Config PDF page with default values
        /// </summary>
        public PdfPageConfig()
        {
            Margins = new PageMargins();
            Size = new PageSize();
            Orientation = PageOrientation.Portrait;
            DisplayHeaderFooter = false;
            PrintBackground = true;
            Scale = 1;
        }

        /// <summary>
        /// Get/Sets the page margins (default 0mm for left/right/top/bottom)
        /// </summary>
        public PageMargins Margins { get; set; }

        /// <summary>
        /// Page Size. <see cref="PageSize"/>. Default value : A4.
        /// </summary>
        public PageSize Size { get; set; }

        /// <summary>
        /// Get/Set Orientation Page PDF. <see cref="PageOrientation"/> Default value : Portrait
        /// </summary>
        public PageOrientation Orientation { get; set; }

        /// <summary>
        /// Get/Set Html Header. Default value : <c>null</c>.
        /// </summary>
        public string? Header { get; set; }

        /// <summary>
        /// Get/Set Html Footer. Default value : <c>null</c>.
        /// </summary>
        public string? Footer { get; set; }

        /// <summary>
        /// Get/Set Display header and footer. Defaults to <c>false</c>.
        /// </summary>
        public bool DisplayHeaderFooter { get; set; }

        /// <summary>
        /// Get/Set Print background graphics. Defaults to <c>true</c>.
        /// </summary>
        public bool PrintBackground { get; set; }

        /// <summary>
        /// Get/Set Scale of the webpage rendering. Defaults to <c>1</c>.
        /// <para>
        /// The scale amount must be between 0.1 and 2.
        /// </para>
        /// </summary>
        public float Scale { get; set; }
    }
}
