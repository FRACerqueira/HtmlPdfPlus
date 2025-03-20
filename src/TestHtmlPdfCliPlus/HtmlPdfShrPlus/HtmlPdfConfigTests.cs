using HtmlPdfPlus;
using HtmlPdfPlus.Client.Core;
namespace TestHtmlPdfPlus.HtmlPdfShrPlus
{
    public class HtmlPdfConfigTests
    {
        [Fact]
        public void DisplayHeaderFooter_SetsValue_ReturnsInstance()
        {
            // Arrange
            var config = new HtmlPdfConfig();

            // Act
            var result = config.DisplayHeaderFooter(true);

            // Assert
            Assert.Equal(config, result);
            Assert.True(config.PageConfig.DisplayHeaderFooter);
        }

        [Fact]
        public void Footer_SetsValue_ReturnsInstance()
        {
            // Arrange
            var config = new HtmlPdfConfig();
            var footer = "Footer content";

            // Act
            var result = config.Footer(footer);

            // Assert
            Assert.Equal(config, result);
            Assert.Equal(footer, config.PageConfig.Footer);
        }

        [Fact]
        public void Format_SetsPageSize_ReturnsInstance()
        {
            // Arrange
            var config = new HtmlPdfConfig();
            var pageSize = PageSize.A4;

            // Act
            var result = config.Format(pageSize);

            // Assert
            Assert.Equal(config, result);
            Assert.Equal(pageSize, config.PageConfig.Size);
        }

        [Fact]
        public void Format_SetsWidthAndHeight_ReturnsInstance()
        {
            // Arrange
            var config = new HtmlPdfConfig();
            decimal width = 210;
            decimal height = 297;

            // Act
            var result = config.Format(width, height);

            // Assert
            Assert.Equal(config, result);
            Assert.Equal(width, config.PageConfig.Size.Width);
            Assert.Equal(height, config.PageConfig.Size.Height);
        }

        [Fact]
        public void Header_SetsValue_ReturnsInstance()
        {
            // Arrange
            var config = new HtmlPdfConfig();
            var header = "Header content";

            // Act
            var result = config.Header(header);

            // Assert
            Assert.Equal(config, result);
            Assert.Equal(header, config.PageConfig.Header);
        }

        [Fact]
        public void Margins_SetsPageMargins_ReturnsInstance()
        {
            // Arrange
            var config = new HtmlPdfConfig();
            var margins = new PageMargins(10, 10, 10, 10);

            // Act
            var result = config.Margins(margins);

            // Assert
            Assert.Equal(config, result);
            Assert.Equal(margins, config.PageConfig.Margins);
        }

        [Fact]
        public void Margins_SetsAllMargins_ReturnsInstance()
        {
            // Arrange
            var config = new HtmlPdfConfig();
            decimal margin = 10;

            // Act
            var result = config.Margins(margin);

            // Assert
            Assert.Equal(config, result);
            Assert.Equal(margin, config.PageConfig.Margins.Top);
            Assert.Equal(margin, config.PageConfig.Margins.Bottom);
            Assert.Equal(margin, config.PageConfig.Margins.Left);
            Assert.Equal(margin, config.PageConfig.Margins.Right);
        }

        [Fact]
        public void Margins_SetsIndividualMargins_ReturnsInstance()
        {
            // Arrange
            var config = new HtmlPdfConfig();
            decimal top = 10;
            decimal bottom = 20;
            decimal left = 30;
            decimal right = 40;

            // Act
            var result = config.Margins(top, bottom, left, right);

            // Assert
            Assert.Equal(config, result);
            Assert.Equal(top, config.PageConfig.Margins.Top);
            Assert.Equal(bottom, config.PageConfig.Margins.Bottom);
            Assert.Equal(left, config.PageConfig.Margins.Left);
            Assert.Equal(right, config.PageConfig.Margins.Right);
        }

        [Fact]
        public void Orientation_SetsValue_ReturnsInstance()
        {
            // Arrange
            var config = new HtmlPdfConfig();
            var orientation = PageOrientation.Landscape;

            // Act
            var result = config.Orientation(orientation);

            // Assert
            Assert.Equal(config, result);
            Assert.Equal(orientation, config.PageConfig.Orientation);
        }

        [Fact]
        public void PreferCSSPageSize_SetsValue_ReturnsInstance()
        {
            // Arrange
            var config = new HtmlPdfConfig();

            // Act
            var result = config.PreferCSSPageSize(true);

            // Assert
            Assert.Equal(config, result);
            Assert.True(config.PageConfig.PreferCSSPageSize);
        }

        [Fact]
        public void PrintBackground_SetsValue_ReturnsInstance()
        {
            // Arrange
            var config = new HtmlPdfConfig();

            // Act
            var result = config.PrintBackground(false);

            // Assert
            Assert.Equal(config, result);
            Assert.False(config.PageConfig.PrintBackground);
        }

        [Fact]
        public void Scale_SetsValue_ReturnsInstance()
        {
            // Arrange
            var config = new HtmlPdfConfig();
            float scale = 1.5f;

            // Act
            var result = config.Scale(scale);

            // Assert
            Assert.Equal(config, result);
            Assert.Equal(scale, config.PageConfig.Scale);
        }

        [Fact]
        public void Scale_InvalidValue_ThrowsArgumentException()
        {
            // Arrange
            var config = new HtmlPdfConfig();
            float invalidScale = 2.5f;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => config.Scale(invalidScale));
            Assert.Equal("Scale amount must be between 0.1 and 2.", exception.Message);
        }
    }// ***************************************************************************************
     // MIT LICENCE
     // The maintenance and evolution is maintained by the HtmlPdfPlus team
     // https://github.com/FRACerqueira/HtmlPdfPlus
     // ***************************************************************************************

    namespace TestHtmlPdfPlus.HtmlPdfShrPlus
    {

    }
}