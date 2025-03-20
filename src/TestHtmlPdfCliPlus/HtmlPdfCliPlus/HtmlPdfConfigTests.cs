// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using HtmlPdfPlus.Client.Core;

namespace TestHtmlPdfPlus.HtmlPdfCliPlus
{
    public class HtmlPdfConfigTests
    {
        private readonly HtmlPdfConfig _config;

        public HtmlPdfConfigTests()
        {
            _config = new HtmlPdfConfig();
        }

        [Fact]
        public void DisplayHeaderFooter_ShouldSetDisplayHeaderFooter()
        {
            _config.DisplayHeaderFooter(true);
            Assert.True(_config.PageConfig.DisplayHeaderFooter);
        }

        [Fact]
        public void Footer_ShouldSetFooter()
        {
            _config.Footer("Footer content");
            Assert.Equal("Footer content", _config.PageConfig.Footer);
        }

        [Fact]
        public void Footer_ShouldSetFooterToNull_WhenValueIsNullOrEmpty()
        {
            _config.Footer(null);
            Assert.Null(_config.PageConfig.Footer);

            _config.Footer(string.Empty);
            Assert.Null(_config.PageConfig.Footer);
        }

        [Fact]
        public void Format_ShouldSetPageSize()
        {
            var pageSize = new PageSize(210, 297);
            _config.Format(pageSize);
            Assert.Equal(pageSize, _config.PageConfig.Size);
        }

        [Fact]
        public void Format_ShouldSetPageSizeWithWidthAndHeight()
        {
            _config.Format(210, 297);
            Assert.Equal(210, _config.PageConfig.Size.Width);
            Assert.Equal(297, _config.PageConfig.Size.Height);
        }

        [Fact]
        public void Format_ShouldThrowArgumentException_WhenWidthOrHeightIsInvalid()
        {
            Assert.Throws<ArgumentException>(() => _config.Format(-1, 297));
            Assert.Throws<ArgumentException>(() => _config.Format(210, -1));
        }

        [Fact]
        public void Header_ShouldSetHeader()
        {
            _config.Header("Header content");
            Assert.Equal("Header content", _config.PageConfig.Header);
        }

        [Fact]
        public void Header_ShouldSetHeaderToNull_WhenValueIsNullOrEmpty()
        {
            _config.Header(null);
            Assert.Null(_config.PageConfig.Header);

            _config.Header(string.Empty);
            Assert.Null(_config.PageConfig.Header);
        }

        [Fact]
        public void Margins_ShouldSetMargins()
        {
            var margins = new PageMargins(10, 10, 10, 10);
            _config.Margins(margins);
            Assert.Equal(margins, _config.PageConfig.Margins);
        }

        [Fact]
        public void Margins_ShouldSetMarginsWithTopBottomLeftRight()
        {
            _config.Margins(10, 20, 30, 40);
            Assert.Equal(10, _config.PageConfig.Margins.Top);
            Assert.Equal(20, _config.PageConfig.Margins.Bottom);
            Assert.Equal(30, _config.PageConfig.Margins.Left);
            Assert.Equal(40, _config.PageConfig.Margins.Right);
        }

        [Fact]
        public void Margins_ShouldThrowArgumentException_WhenAnyMarginValueIsInvalid()
        {
            Assert.Throws<ArgumentException>(() => _config.Margins(-1, 20, 30, 40));
            Assert.Throws<ArgumentException>(() => _config.Margins(10, -1, 30, 40));
            Assert.Throws<ArgumentException>(() => _config.Margins(10, 20, -1, 40));
            Assert.Throws<ArgumentException>(() => _config.Margins(10, 20, 30, -1));
        }

        [Fact]
        public void Margins_ShouldSetMarginsWithSingleValue()
        {
            _config.Margins(10);
            Assert.Equal(10, _config.PageConfig.Margins.Top);
            Assert.Equal(10, _config.PageConfig.Margins.Bottom);
            Assert.Equal(10, _config.PageConfig.Margins.Left);
            Assert.Equal(10, _config.PageConfig.Margins.Right);
        }

        [Fact]
        public void Margins_ShouldThrowArgumentException_WhenSingleMarginValueIsInvalid()
        {
            Assert.Throws<ArgumentException>(() => _config.Margins(-1));
        }

        [Fact]
        public void Orientation_ShouldSetOrientation()
        {
            _config.Orientation(PageOrientation.Landscape);
            Assert.Equal(PageOrientation.Landscape, _config.PageConfig.Orientation);
        }

        [Fact]
        public void PreferCSSPageSize_ShouldSetPreferCSSPageSize()
        {
            _config.PreferCSSPageSize(true);
            Assert.True(_config.PageConfig.PreferCSSPageSize);
        }

        [Fact]
        public void PrintBackground_ShouldSetPrintBackground()
        {
            _config.PrintBackground(false);
            Assert.False(_config.PageConfig.PrintBackground);
        }

        [Fact]
        public void Scale_ShouldSetScale()
        {
            _config.Scale(1.5f);
            Assert.Equal(1.5f, _config.PageConfig.Scale);
        }

        [Fact]
        public void Scale_ShouldThrowArgumentException_WhenScaleValueIsOutOfRange()
        {
            Assert.Throws<ArgumentException>(() => _config.Scale(0.05f));
            Assert.Throws<ArgumentException>(() => _config.Scale(2.5f));
        }
    }

}
