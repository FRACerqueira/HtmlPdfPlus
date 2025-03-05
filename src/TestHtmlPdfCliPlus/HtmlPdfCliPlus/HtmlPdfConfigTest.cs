// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfCliPlus.Core;
using HtmlPdfPlus;

namespace TestHtmlPdfPlus.HtmlPdfCliPlus
{
    public class HtmlPdfConfigTest
    {

        [Fact]
        public void Ensure_Create_HtmlPdfConfig_FormatByWH()
        {
            var result = new HtmlPdfConfig();
            result.Format(); 
            Assert.Equal("210.0;297.0", result.PageConfig.Size.ToString());
        }

        [Fact]
        public void Ensure_Create_HtmlPdfConfig_FormatByPageSize()
        {
            var result = new HtmlPdfConfig();
            result.Format(PageSize.Legal);
            Assert.Equal("215.9;355.6", result.PageConfig.Size.ToString());
        }

        [Fact]
        public void Ensure_Create_HtmlPdfConfig_Footer()
        {
            var result = new HtmlPdfConfig();
            result.Footer("<h2>teste</h2>");
            Assert.Equal("<h2>teste</h2>", result.PageConfig.Footer);
        }

        [Fact]
        public void Ensure_Create_HtmlPdfConfig_FooterNUll()
        {
            var result = new HtmlPdfConfig();
            result.Footer("<h2>teste</h2>");
            result.Footer(null);
            Assert.Null(result.PageConfig.Footer);
        }

        [Fact]
        public void Ensure_Create_HtmlPdfConfig_Header()
        {
            var result = new HtmlPdfConfig();
            result.Header("<h2>teste</h2>");
            Assert.Equal("<h2>teste</h2>", result.PageConfig.Header);
        }

        [Fact]
        public void Ensure_Create_HtmlPdfConfig_HeaderNull()
        {
            var result = new HtmlPdfConfig();
            result.Header("<h2>teste</h2>\n");
            result.Header(null);
            Assert.Null(result.PageConfig.Footer);
        }

        [Fact]
        public void Ensure_Create_HtmlPdfConfig_Margins()
        {
            var result = new HtmlPdfConfig();
            result.Margins(1,2,3,4);
            Assert.Equal("1.0;2.0;3.0;4.0", result.PageConfig.Margins.ToString());
        }

        [Fact]
        public void Ensure_Create_HtmlPdfConfig_MarginsClass()
        {
            var result = new HtmlPdfConfig();
            result.Margins(PageMargins.Create(1,2,3,4));
            Assert.Equal("1.0;2.0;3.0;4.0", result.PageConfig.Margins.ToString());
        }

        [Theory]
        [InlineData(PageOrientation.Landscape)]
        [InlineData(PageOrientation.Portrait)]
        public void Ensure_Create_HtmlPdfConfig_Orientation(PageOrientation orientation)
        {
            var result = new HtmlPdfConfig();
            result.Orientation(orientation);
            Assert.Equal(orientation, result.PageConfig.Orientation);
        }

    }
}
