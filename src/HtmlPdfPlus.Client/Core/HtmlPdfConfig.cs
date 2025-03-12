// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

namespace HtmlPdfPlus.Client.Core
{
    /// <summary>
    /// Configuration for HTML to PDF conversion.
    /// </summary>
    internal sealed class HtmlPdfConfig : IPdfPageConfig
    {
        internal PdfPageConfig PageConfig { get; } = new();

        /// <summary>
        /// Sets whether to display the header and footer.
        /// </summary>
        /// <param name="value">Display header and footer? Default is <c>false</c>.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        public IPdfPageConfig DisplayHeaderFooter(bool value = false)
        {
            PageConfig.DisplayHeaderFooter = value;
            return this;
        }

        /// <summary>
        /// Sets the HTML footer.
        /// </summary>
        /// <param name="value">HTML footer. Default is <c>null</c>.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        public IPdfPageConfig Footer(string? value)
        {
            PageConfig.Footer = !string.IsNullOrEmpty(value) ? value : null;
            return this;
        }

        /// <summary>
        /// Sets the page size of the rendered document.
        /// </summary>
        /// <param name="value"><see cref="PageSize"/>. Default is A4.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        public IPdfPageConfig Format(PageSize value)
        {
            PageConfig.Size = value;
            return this;
        }

        /// <summary>
        /// Sets the page size of the rendered document.
        /// </summary>
        /// <param name="width">Page width in millimeters. Default is 210mm.</param>
        /// <param name="height">Page height in millimeters. Default is 297mm.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when width or height is invalid.</exception>
        public IPdfPageConfig Format(decimal width = 210, decimal height = 297)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Width and height must be positive values.");
            }
            PageConfig.Size = new PageSize(width, height);
            return this;
        }

        /// <summary>
        /// Sets the HTML header.
        /// </summary>
        /// <param name="value">HTML header. Default is <c>null</c>.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        public IPdfPageConfig Header(string? value)
        {
            PageConfig.Header = !string.IsNullOrEmpty(value) ? value : null;
            return this;
        }

        /// <summary>
        /// Sets the page margins.
        /// </summary>
        /// <param name="value">Margins <see cref="PageMargins"/>. Default is 0mm for all sides.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        public IPdfPageConfig Margins(PageMargins value)
        {
            PageConfig.Margins = value;
            return this;
        }

        /// <summary>
        /// Sets the page margins.
        /// </summary>
        /// <param name="top">Margin top in millimeters. Default is 0mm.</param>
        /// <param name="bottom">Margin bottom in millimeters. Default is 0mm.</param>
        /// <param name="left">Margin left in millimeters. Default is 0mm.</param>
        /// <param name="right">Margin right in millimeters. Default is 0mm.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when any margin value is invalid.</exception>
        public IPdfPageConfig Margins(decimal top = 0, decimal bottom = 0, decimal left = 0, decimal right = 0)
        {
            if (top < 0 || bottom < 0 || left < 0 || right < 0)
            {
                throw new ArgumentException("Margin values must be non-negative.");
            }
            PageConfig.Margins = new PageMargins(top, bottom, left, right);
            return this;
        }

        /// <summary>
        /// Sets the page margins.
        /// </summary>
        /// <param name="value">Margin for all sides in millimeters. Default is 0mm.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the margin value is invalid.</exception>
        public IPdfPageConfig Margins(decimal value)
        {
            if (value < 0)
            {
                throw new ArgumentException("Margin value must be non-negative.");
            }
            PageConfig.Margins = new PageMargins(value, value, value, value);
            return this;
        }

        /// <summary>
        /// Sets the page orientation.
        /// </summary>
        /// <param name="value"><see cref="PageOrientation"/>. Default is Portrait.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        public IPdfPageConfig Orientation(PageOrientation value)
        {
            PageConfig.Orientation = value;
            return this;
        }

        /// <summary>
        /// Sets whether to print background graphics.
        /// </summary>
        /// <param name="value">Print background images? Default is <c>true</c>.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        public IPdfPageConfig PrintBackground(bool value = true)
        {
            PageConfig.PrintBackground = value;
            return this;
        }

        /// <summary>
        /// Sets the scale of the webpage rendering.
        /// </summary>
        /// <param name="value">The scale. Must be between 0.1 and 2. Default is 1.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the scale value is out of range.</exception>
        public IPdfPageConfig Scale(float value)
        {
            if (value < 0.1 || value > 2)
            {
                throw new ArgumentException("Scale amount must be between 0.1 and 2.");
            }
            PageConfig.Scale = value;
            return this;
        }
    }
}
