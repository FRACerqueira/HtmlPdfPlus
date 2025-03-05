// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;

namespace HtmlPdfShrPlus.Core
{
    internal sealed class HtmlPdfConfig : IPdfPageConfig
    {
        internal readonly PdfPageConfig PageConfig = new();

        public IPdfPageConfig DisplayHeaderFooter(bool value = false)
        {
            PageConfig.DisplayHeaderFooter = value;
            return this;
        }

        public IPdfPageConfig Footer(string? value)
        {
            PageConfig.Footer = value;
            return this;
        }

        public IPdfPageConfig Format(PageSize value)
        {
            PageConfig.Size = value;
            return this;
        }

        public IPdfPageConfig Format(decimal width = 210, decimal height = 297)
        {
            PageConfig.Size = new PageSize(width, height);
            return this;
        }

        public IPdfPageConfig Header(string? value)
        {
            PageConfig.Header = value;
            return this;
        }

        public IPdfPageConfig Margins(PageMargins value)
        {
            PageConfig.Margins = value;
            return this;
        }

        public IPdfPageConfig Margins(decimal top = 0, decimal bottom = 0, decimal left = 0, decimal right = 0)
        {
            PageConfig.Margins = new PageMargins(top, bottom, left, right);
            return this;
        }

        public IPdfPageConfig Margins(decimal value = 0)
        {
            PageConfig.Margins = new PageMargins(value, value, value, value);
            return this;
        }

        public IPdfPageConfig Orientation(PageOrientation value)
        {
            PageConfig.Orientation = value;
            return this;
        }

        public IPdfPageConfig PrintBackground(bool value = true)
        {
            PageConfig.PrintBackground = value;
            return this;
        }

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
