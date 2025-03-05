// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

namespace HtmlPdfPlus
{
    /// <summary>
    /// Fluent interface commands to configure PDF rendering.
    /// </summary>
    public interface IPdfPageConfig
    {
        /// <summary>
        /// Sets the page margins.
        /// </summary>
        /// <param name="value">Margins <see cref="PageMargins"/>. Default is 0mm for all sides.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        IPdfPageConfig Margins(PageMargins value);

        /// <summary>
        /// Sets the page margins.
        /// </summary>
        /// <param name="value">Margin for all sides in millimeters. Default is 0mm.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the margin value is invalid.</exception>
        IPdfPageConfig Margins(decimal value = 0);

        /// <summary>
        /// Sets the page margins.
        /// </summary>
        /// <param name="top">Margin top in millimeters. Default is 0mm.</param>
        /// <param name="bottom">Margin bottom in millimeters. Default is 0mm.</param>
        /// <param name="left">Margin left in millimeters. Default is 0mm.</param>
        /// <param name="right">Margin right in millimeters. Default is 0mm.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when any margin value is invalid.</exception>
        IPdfPageConfig Margins(decimal top = 0, decimal bottom = 0, decimal left = 0, decimal right = 0);

        /// <summary>
        /// Sets the page size of the rendered document.
        /// </summary>
        /// <param name="value"><see cref="PageSize"/>. Default is A4.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        IPdfPageConfig Format(PageSize value);

        /// <summary>
        /// Sets the page size of the rendered document.
        /// </summary>
        /// <param name="width">Page width in millimeters. Default is 210mm.</param>
        /// <param name="height">Page height in millimeters. Default is 297mm.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when width or height is invalid.</exception>
        IPdfPageConfig Format(decimal width = 210, decimal height = 297);

        /// <summary>
        /// Sets the page orientation.
        /// </summary>
        /// <param name="value"><see cref="PageOrientation"/>. Default is Portrait.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        IPdfPageConfig Orientation(PageOrientation value);

        /// <summary>
        /// Sets the HTML header.
        /// </summary>
        /// <param name="value">HTML header. Default is <c>null</c>.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        IPdfPageConfig Header(string? value);

        /// <summary>
        /// Sets the HTML footer.
        /// </summary>
        /// <param name="value">HTML footer. Default is <c>null</c>.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        IPdfPageConfig Footer(string? value);

        /// <summary>
        /// Sets whether to display the header and footer.
        /// </summary>
        /// <param name="value">Display header and footer? Default is <c>false</c>.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        IPdfPageConfig DisplayHeaderFooter(bool value = false);

        /// <summary>
        /// Sets whether to print background graphics.
        /// </summary>
        /// <param name="value">Print background images? Default is <c>true</c>.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        IPdfPageConfig PrintBackground(bool value = true);

        /// <summary>
        /// Sets the scale of the webpage rendering.
        /// </summary>
        /// <param name="value">The scale. Must be between 0.1 and 2. Default is 1.</param>
        /// <returns><see cref="IPdfPageConfig"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the scale value is out of range.</exception>
        IPdfPageConfig Scale(float value);
    }
}
