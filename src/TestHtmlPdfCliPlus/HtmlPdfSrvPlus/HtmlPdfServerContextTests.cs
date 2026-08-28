// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using HtmlPdfPlus.Server.Core;

namespace TestHtmlPdfPlus.HtmlPdfSrvPlus
{
    public class HtmlPdfServerContextTests
    {
        private const string TemplateWithExtraWhitespace = "<html>\r\n\r\n   <body>\r\n      <h1>@Model</h1>\r\n\r\n\r\n   </body>\r\n\r\n</html>";

        [Fact]
        public void FromRazor_DefaultMinify_ProducesMinifiedHtml()
        {
            // Arrange
            using var builder = new HtmlPdfBuilder(null);
            var context = (HtmlPdfServerContext<object, byte[]>)new HtmlPdfServer<object, byte[]>(builder, "test").ScopeData(null);

            // Act - minify defaults to true, per IHtmlPdfServerContext.FromRazor's own doc.
            context.FromRazor(TemplateWithExtraWhitespace, "hi");

            // Assert - the template's extra blank lines between tags must be gone.
            Assert.DoesNotContain("\r\n\r\n", context.Html);
        }

        [Fact]
        public void FromRazor_MinifyFalse_KeepsRawUnminifiedHtml()
        {
            // Arrange
            using var builder = new HtmlPdfBuilder(null);
            var context = (HtmlPdfServerContext<object, byte[]>)new HtmlPdfServer<object, byte[]>(builder, "test").ScopeData(null);

            // Act
            context.FromRazor(TemplateWithExtraWhitespace, "hi", minify: false);

            // Assert - minify:false must preserve the template's own whitespace verbatim.
            Assert.Contains("\r\n\r\n", context.Html);
        }
    }
}
