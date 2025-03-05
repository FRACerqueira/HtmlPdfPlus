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
    /// Page size for PDF.
    /// </summary>
    public sealed class PageSize
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PageSize"/> class with default value : A4.
        /// Page width and height in millimeters.
        /// </summary>
        public PageSize()
        {
            // A4 SIZE
            Width = 210;
            Height = 297;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageSize"/> class.
        /// Page width and height in millimeters.
        /// </summary>
        /// <param name="width">Page width in millimeters.</param>
        /// <param name="height">Page height in millimeters.</param>
        public PageSize(decimal width, decimal height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Width or height must be greater than zero.");
            }
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Create a new instance of the <see cref="PageSize"/> class.
        /// Page width and height in millimeters.
        /// </summary>
        /// <param name="width">Page width in millimeters.</param>
        /// <param name="height">Page height in millimeters.</param>
        /// <returns><see cref="PageSize"/> instance</returns>
        public static PageSize Create(decimal width, decimal height) => new(width, height);

        /// <summary>
        /// Page width in millimeters.
        /// </summary>
        /// <value>The width.</value>
        [JsonInclude]
        public decimal Width { get; private set; }

        /// <summary>
        /// Page height in millimeters.
        /// </summary>
        /// <value>The height.</value>
        [JsonInclude]
        public decimal Height { get; private set; }

        private static readonly PageSize a0 = new(841, 1189);
        private static readonly PageSize a1 = new(594, 841);
        private static readonly PageSize a2 = new(420, 594);
        private static readonly PageSize a3 = new(297, 420);
        private static readonly PageSize a4 = new(210, 297);
        private static readonly PageSize a5 = new(148, 210);
        private static readonly PageSize a6 = new(105, 148);
        private static readonly PageSize b0 = new(1000, 1414);
        private static readonly PageSize b1 = new(707, 1000);
        private static readonly PageSize b2 = new(500, 707);
        private static readonly PageSize b3 = new(353, 500);
        private static readonly PageSize b4 = new(250, 353);
        private static readonly PageSize b5 = new(176, 250);
        private static readonly PageSize b6 = new(125, 176);
        private static readonly PageSize legal = new(215.9m, 355.6m);
        private static readonly PageSize letter = new(215.9m, 279.4m);
        private static readonly PageSize tabloid = new(279.4m, 431.8m);

        /// <summary>
        /// A0: 841 x 1189 mm 
        /// </summary>
        public static PageSize A0 => a0;

        /// <summary>
        /// A1: 594 x 841 mm 
        /// </summary>
        public static PageSize A1 => a1;

        /// <summary>
        /// A2: 420 x 594 mm 
        /// </summary>
        public static PageSize A2 => a2;

        /// <summary>
        /// A3: 297 x 420 mm 
        /// </summary>
        public static PageSize A3 => a3;

        /// <summary>
        /// A4: 210 x 297 mm 
        /// </summary>
        public static PageSize A4 => a4;

        /// <summary>
        /// A5: 148 x 210 mm 
        /// </summary>
        public static PageSize A5 => a5;

        /// <summary>
        /// A6: 105 x 148 mm 
        /// </summary>
        public static PageSize A6 => a6;

        /// <summary>
        /// B0: 1000 x 1414 mm 
        /// </summary>
        public static PageSize B0 => b0;

        /// <summary>
        /// B1: 707 x 1000 mm 
        /// </summary>
        public static PageSize B1 => b1;

        /// <summary>
        /// B2: 500 x 707 mm 
        /// </summary>
        public static PageSize B2 => b2;

        /// <summary>
        /// B3: 353 x 500 mm 
        /// </summary>
        public static PageSize B3 => b3;

        /// <summary>
        /// B4: 250 x 353 mm 
        /// </summary>
        public static PageSize B4 => b4;

        /// <summary>
        /// B5: 176 x 250 mm 
        /// </summary>
        public static PageSize B5 => b5;

        /// <summary>
        /// B6: 125 x 176 mm 
        /// </summary>
        public static PageSize B6 => b6;

        /// <summary>
        /// Legal: 215.9mm x 355.6 mm 
        /// </summary>
        public static PageSize Legal => legal;

        /// <summary>
        /// Letter: 215.9 x 279.4 mm 
        /// </summary>
        public static PageSize Letter => letter;

        /// <summary>
        /// Tabloid : 279.4 x 431.8 mm
        /// </summary>
        public static PageSize Tabloid => tabloid;

        /// <summary>
        /// Representation of size in string format
        /// </summary>
        /// <returns>size in string format, separated by semicolons (Width;Height)</returns>
        public override string ToString()
        {
            return string.Join(
                ';',
                Width.ToString("0.0", CultureInfo.InvariantCulture),
                Height.ToString("0.0", CultureInfo.InvariantCulture));
        }
    }
}
