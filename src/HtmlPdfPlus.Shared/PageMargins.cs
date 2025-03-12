// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Globalization;
using System.Text.Json.Serialization;

namespace HtmlPdfPlus
{
    /// <summary>
    /// Page margins.
    /// </summary>
    public sealed class PageMargins
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PageMargins"/> class with default value : 0mm for all.
        /// Margins pages in millimeters: top, bottom, left, right. 
        /// </summary>
        public PageMargins()
        {
            Top = 0;
            Bottom = 0;
            Left = 0;
            Right = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageMargins"/> class.
        /// Margins pages in millimeters: top, bottom, left, right.
        /// </summary>
        /// <param name="top">Margin top in millimeters.</param>
        /// <param name="bottom">Margin bottom in millimeters.</param>
        /// <param name="left">Margin left in millimeters.</param>
        /// <param name="right">Margin right in millimeters.</param>
        public PageMargins(decimal top, decimal bottom, decimal left, decimal right)
        {
            if (top < 0 || bottom < 0 || left < 0 || right < 0)
            {
                throw new ArgumentException("Margins must be greater than or equal to zero.");
            }
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Create a new instance of the <see cref="PageMargins"/> class.
        /// </summary>
        /// <param name="top">Margin top in millimeters.</param>
        /// <param name="bottom">Margin bottom in millimeters.</param>
        /// <param name="left">Margin left in millimeters.</param>
        /// <param name="right">Margin right in millimeters.</param>
        /// <returns><see cref="PageMargins"/> instance</returns>
        public static PageMargins Create(decimal top, decimal bottom, decimal left, decimal right) => new(top, bottom, left, right);

        /// <summary>
        /// Page width in millimeters.
        /// </summary>
        /// <value>The width.</value>
        [JsonInclude]
        public decimal Top { get; private set; }

        /// <summary>
        /// Page height in millimeters.
        /// </summary>
        /// <value>The Height.</value>
        [JsonInclude]
        public decimal Bottom { get; private set; }

        /// <summary>
        /// Page height in millimeters.
        /// </summary>
        /// <value>The Height.</value>
        [JsonInclude]
        public decimal Left { get; private set; }

        /// <summary>
        /// Page height in millimeters.
        /// </summary>
        /// <value>The Height.</value>
        [JsonInclude]
        public decimal Right { get; private set; }

        /// <summary>
        /// Representation of margins in string format
        /// </summary>
        /// <returns>All margins in string format, separated by semicolons (top;bottom;left;right)</returns>
        public override string ToString()
        {
            return string.Join(
                ';',
                Top.ToString("0.0", CultureInfo.InvariantCulture),
                Bottom.ToString("0.0", CultureInfo.InvariantCulture),
                Left.ToString("0.0", CultureInfo.InvariantCulture),
                Right.ToString("0.0", CultureInfo.InvariantCulture));
        }
    }
}
