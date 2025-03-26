// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using HtmlPdfPlus.Shared.Core;
using NUglify;
using System.Text;
using System.Text.Json;

namespace TestHtmlPdfPlus.HtmlPdfShrPlus
{
    public class RequestHtmlPdfTests
    {
        [Fact]
        public void Constructor_ValidParameters_ShouldInitializeProperties()
        {
            // Arrange
            var html = "<html></html>";
            var alias = "testAlias";
            var config = new PdfPageConfig();
            var timeout = 30000;
            var inputParam = "input";

            // Act
            var request = new RequestHtmlPdf<string>(html, alias, config, timeout, inputParam);

            // Assert
            Assert.Equal(html, request.Html);
            Assert.Equal(alias, request.Alias);
            Assert.Equal(config, request.Config);
            Assert.Equal(timeout, request.Timeout);
            Assert.Equal(inputParam, request.InputParam);
        }

        [Fact]
        public void Constructor_EmptyHtml_ShouldThrowArgumentException()
        {
            // Arrange
            var html = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new RequestHtmlPdf<string>(html));
        }

        [Fact]
        public void Constructor_TimeoutLessThanOrEqualToZero_ShouldThrowArgumentException()
        {
            // Arrange
            var html = "<html></html>";
            var timeout = 0;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new RequestHtmlPdf<string>(html, timeout: timeout));
        }

        [Fact]
        public void ChangeHtml_ValidHtml_ShouldUpdateHtml()
        {
            // Arrange
            var html = "<html></html>";
            var request = new RequestHtmlPdf<string>(html);
            var newHtml = "<html><body>Updated</body></html>";

            // Act
            request.ChangeHtml(newHtml, false);

            // Assert
            Assert.Equal(newHtml, request.Html);
        }

        [Fact]
        public void ChangeHtml_EmptyHtml_ShouldThrowArgumentException()
        {
            // Arrange
            var html = "<html></html>";
            var request = new RequestHtmlPdf<string>(html);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => request.ChangeHtml("", false));
        }

        [Fact]
        public void ChangeHtml_Minify_ShouldMinifyHtml()
        {
            // Arrange
            var html = "<html>   <body>   Test   </body>   </html>";
            var request = new RequestHtmlPdf<string>(html);
            var expectedHtml = Uglify.Html(html).Code;

            // Act
            request.ChangeHtml(html, true);

            // Assert
            Assert.Equal(expectedHtml, request.Html);
        }

        [Fact]
        public void ToBytes_ShouldReturnByteArray()
        {
            // Arrange
            var html = "<html></html>";
            var request = new RequestHtmlPdf<string>(html);
            var expectedBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

            // Act
            var result = request.ToBytes();

            // Assert
            Assert.Equal(expectedBytes, result);
        }

        [Fact]
        public async Task ToBytesCompress_ShouldReturnCompressedByteArray()
        {
            // Arrange
            var html = "<html></html>";
            var request = new RequestHtmlPdf<string>(html);
            var bytes = request.ToBytes();
            var expectedCompressedBytes = await GZipHelper.CompressAsync(bytes);

            // Act
            var result = await request.ToBytesCompress();

            // Assert
            Assert.Equal(expectedCompressedBytes, result);
        }

        [Fact]
        public void FromBytes_ShouldReturnRequestHtmlPdf()
        {
            // Arrange
            var html = "<html>   <body>   Test   </body>   </html>";
            var request = new RequestHtmlPdf<string>(html);
            var bytes = request.ToBytes();

            // Act
            var result = RequestHtmlPdf<string>.FromBytes(bytes);

            // Assert
            Assert.Equal(request.Html, result.Html);
            Assert.Equal(request.Alias, result.Alias);
            Assert.Equal(request.Config, result.Config);
            Assert.Equal(request.Timeout, result.Timeout);
            Assert.Equal(request.InputParam, result.InputParam);
        }

        [Fact]
        public async Task FromBytesCompress_ShouldReturnRequestHtmlPdf()
        {
            // Arrange
            var html = "<html>   <body>   Test   </body>   </html>";
            var request = new RequestHtmlPdf<string>(html);
            var bytes = await GZipHelper.CompressAsync(request.ToBytes());

            // Act
            var result = RequestHtmlPdf<string>.FromBytesCompress(bytes);

            // Assert
            Assert.Equal(request.Html, result.Html);
            Assert.Equal(request.Alias, result.Alias);
            Assert.Equal(request.Config, result.Config);
            Assert.Equal(request.Timeout, result.Timeout);
            Assert.Equal(request.InputParam, result.InputParam);
        }
    }
}
