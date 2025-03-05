// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;

namespace TestHtmlPdfPlus.HtmlPdfShrPlus
{
    public class PageSizeTest
    {
        [Fact]
        public void DefaultConstructor_SetsA4Size()
        {
            // Act
            var pageSize = new PageSize();

            // Assert
            Assert.Equal(210, pageSize.Width);
            Assert.Equal(297, pageSize.Height);
        }

        [Fact]
        public void ParameterizedConstructor_SetsWidthAndHeight()
        {
            // Act
            var pageSize = new PageSize(100, 200);

            // Assert
            Assert.Equal(100, pageSize.Width);
            Assert.Equal(200, pageSize.Height);
        }

        [Fact]
        public void ParameterizedConstructor_ThrowsArgumentException_WhenWidthOrHeightIsZeroOrNegative()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new PageSize(0, 200));
            Assert.Throws<ArgumentException>(() => new PageSize(100, 0));
            Assert.Throws<ArgumentException>(() => new PageSize(-100, 200));
           
            Assert.Throws<ArgumentException>(() => new PageSize(100, -200));
        }

        [Fact]
        public void CreateMethod_ReturnsNewInstanceWithGivenDimensions()
        {
            // Act
            var pageSize = PageSize.Create(150, 250);

            // Assert
            Assert.Equal(150, pageSize.Width);
            Assert.Equal(250, pageSize.Height);
        }

        [Fact]
        public void ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var pageSize = new PageSize(123.45m, 678.90m);

            // Act
            var result = pageSize.ToString();

            // Assert
            Assert.Equal("123.5;678.9", result);
        }

        [Fact]
        public void StaticProperties_ReturnCorrectSizes()
        {
            // Assert
            Assert.Equal(new PageSize(841.0m, 1189.0m).ToString(), PageSize.A0.ToString());
            Assert.Equal(new PageSize(594.0m, 841.0m).ToString(), PageSize.A1.ToString());
            Assert.Equal(new PageSize(420.0m, 594.0m).ToString(), PageSize.A2.ToString());
            Assert.Equal(new PageSize(297.0m, 420.0m).ToString(), PageSize.A3.ToString());
            Assert.Equal(new PageSize(210.0m, 297.0m).ToString(), PageSize.A4.ToString());
            Assert.Equal(new PageSize(148.0m, 210.0m).ToString(), PageSize.A5.ToString());
            Assert.Equal(new PageSize(105.0m, 148.0m).ToString(), PageSize.A6.ToString());
            Assert.Equal(new PageSize(1000.0m, 1414.0m).ToString(), PageSize.B0.ToString());
            Assert.Equal(new PageSize(707.0m, 1000.0m).ToString(), PageSize.B1.ToString());
            Assert.Equal(new PageSize(500.0m, 707.0m).ToString(), PageSize.B2.ToString());
            Assert.Equal(new PageSize(353.0m, 500.0m).ToString(), PageSize.B3.ToString());
            Assert.Equal(new PageSize(250.0m, 353.0m).ToString(), PageSize.B4.ToString());
            Assert.Equal(new PageSize(176.0m, 250.0m).ToString(), PageSize.B5.ToString());
            Assert.Equal(new PageSize(125.0m, 176.0m).ToString(), PageSize.B6.ToString());
            Assert.Equal(new PageSize(215.9m, 355.6m).ToString(), PageSize.Legal.ToString());
            Assert.Equal(new PageSize(215.9m, 279.4m).ToString(), PageSize.Letter.ToString());
            Assert.Equal(new PageSize(279.4m, 431.8m).ToString(), PageSize.Tabloid.ToString());
        }


    }
}
