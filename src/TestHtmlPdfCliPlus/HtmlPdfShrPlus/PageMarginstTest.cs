// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;

namespace TestHtmlPdfPlus.HtmlPdfShrPlus
{
    public class PageMarginstTest
    {

        [Fact]
        public void Ensure_Serialization_PageMargins_default()
        {
            var result = new PageMargins().ToString();
            Assert.Equal("0.0;0.0;0.0;0.0", result);
        }

        [Fact]
        public void Ensure_Serialization_PageMargins_Custom()
        {
            var result = new PageMargins(1, 2, 3, 4).ToString();
            Assert.Equal("1.0;2.0;3.0;4.0", result);
        }


        [Fact]
        public void Ensure_Serialization_PageMargins_ValidateTop()
        {
            Assert.Throws<ArgumentException>(() => new PageMargins(-1, 2, 3, 4));
        }

        [Fact]
        public void Ensure_Serialization_PageMargins_Validatebottom()
        {
            Assert.Throws<ArgumentException>(() => new PageMargins(1, -2, 3, 4));
        }
        [Fact]
        public void Ensure_Serialization_PageMargins_Validateleft()
        {
            Assert.Throws<ArgumentException>(() => new PageMargins(1, 2, -3, 4));
        }
        [Fact]
        public void Ensure_Serialization_PageMargins_ValidateRight()
        {
            Assert.Throws<ArgumentException>(() => new PageMargins(1, 2, 3, -4));
        }

    }
}
