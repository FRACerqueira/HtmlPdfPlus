﻿// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using HtmlPdfPlus.Server.Core;

namespace TestHtmlPdfPlus.HtmlPdfSrvPlus
{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    public class HtmlPdfServerTests
    {
        [Fact]
        public void BeforePDF_ThrowsArgumentNullException_WhenInputParamIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new HtmlPdfServer<object, byte[]>(null, "teste").ScopeData(null).BeforePDF(null));
        }


        [Fact]
        public void AfterPDF_ThrowsArgumentNullException_WhenInputParamIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new HtmlPdfServer<object, byte[]>(null, "teste").ScopeData(null).AfterPDF(null));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenPdfSrvBuilderIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new HtmlPdfServer<string, byte[]>(null, "validAlias"));
        }

        [Fact]
        public void Constructor_SetsProperties_WhenParametersAreValid()
        {
            // Arrange
            using var objbuilder = new HtmlPdfBuilder(null);
            var sourceAlias = "validAlias";
            // Act
            var server = new HtmlPdfServer<string, byte[]>(objbuilder, sourceAlias);
            // Assert
            Assert.NotNull(server);
        }

        [Fact]
        public async Task Run_ThrowsArgumentNullException_WhenRequestclientIsNull()
        {
            // Arrange
            using var objbuilder = new HtmlPdfBuilder(null);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await new HtmlPdfServer<string, byte[]>(objbuilder, "Test").Run(null, CancellationToken.None));
        }

        [Fact]
        public async Task Run_ThrowsArgumentNullException_WhenRequestclientIsEmpty()
        {
            // Arrange
            using var objbuilder = new HtmlPdfBuilder(null);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await new HtmlPdfServer<string, byte[]>(objbuilder, "Test").Run([], CancellationToken.None));
        }

        [Fact]
        public async Task Run_ThrowsArgumentNullException_WhenNotExistfterPDFAndReturnCustomType()
        {
            // Arrange
            using var objbuilder = new HtmlPdfBuilder(null);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await new HtmlPdfServer<string, string>(objbuilder, "Teste").Run(
                await new RequestHtmlPdf<string>("","Teste", new PdfPageConfig(), 10000).ToBytesCompress(), CancellationToken.None));
        }


        [Fact]
        public async Task Run_Resultfalse_WhenErrorOnBeforePDF()
        {
            // Arrange
            using var objbuilder = new HtmlPdfBuilder(null);
            var requestHtmlPdf = await new RequestHtmlPdf<object>("<h1>Test</h1>","teste", new PdfPageConfig(),10000).ToBytesCompress();
            // Act & Assert
            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, "Server")
                .ScopeRequest(requestHtmlPdf)
                .BeforePDF((_, _, _) => throw new InvalidTimeZoneException("Test"))
                .Run(CancellationToken.None);
            Assert.IsType<InvalidTimeZoneException>(result.Error);
            Assert.False(result.IsSuccess);
            Assert.True(result.ElapsedTime.TotalMilliseconds > 0);
            Assert.Null(result.OutputData);
        }

        [Fact]
        public async Task Run_ResultTrue_BasicPDF()
        {
            // Arrange
            using var objbuilder = new HtmlPdfBuilder(null);
            await objbuilder.BuildAsync("Server");
            var config = new PdfPageConfig
            {
                Margins = new PageMargins(10, 10, 10, 10),
                DisplayHeaderFooter = true,
                Orientation = PageOrientation.Landscape,
                Size = PageSize.A3
            };
            var requestHtmlPdf = await new RequestHtmlPdf<byte[]>("<h1>Test</h1>", "teste", new PdfPageConfig(), 5000).ToBytesCompress();

            // Act & Assert
            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, "Server")
                .Run(requestHtmlPdf, CancellationToken.None);
            Assert.True(result.IsSuccess);
            Assert.Null(result.Error);
            Assert.NotNull(result.OutputData);
            Assert.True(result.OutputData.Length > 0);
            Assert.True(result.ElapsedTime.TotalMilliseconds > 0);

        }

        [Fact]
        public async Task Run_ResultTrue_WithBeforePDF_AND_AfterPDF()
        {
            // Arrange
            using var objbuilder = new HtmlPdfBuilder(null);
            await objbuilder.BuildAsync("Server");
            var config = new PdfPageConfig
            {
                Margins = new PageMargins(10, 10, 10, 10),
                DisplayHeaderFooter = true,
                Orientation = PageOrientation.Landscape,
                Size = PageSize.A3,
            };
            var requestHtmlPdf = await new RequestHtmlPdf<byte[]>("<h1>Test</h1>", "teste", new PdfPageConfig(), 5000).ToBytesCompress();

            // Act & Assert
            var result = await new HtmlPdfServer<object, string>(objbuilder, "Server")
                .ScopeRequest(requestHtmlPdf)
                .BeforePDF((_,_,_) => Task.FromResult<string>("<h3>Test</h3>"))
                .AfterPDF((_,_,_) => Task.FromResult<string>("Test"))
                .Run(CancellationToken.None);
            Assert.True(result.IsSuccess);
            Assert.Null(result.Error);
            Assert.True(result.ElapsedTime.TotalMilliseconds > 0);
            Assert.NotNull(result.OutputData);
            Assert.Equal("Test",result.OutputData);
        }
    }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
}
