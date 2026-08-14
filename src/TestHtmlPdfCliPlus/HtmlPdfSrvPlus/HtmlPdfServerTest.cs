// ***************************************************************************************
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
            Assert.NotNull(result.Error);
            Assert.Equal(ErrorCode.Internal, result.Error!.Code);
            Assert.Equal("Test", result.Error.Message);
            Assert.False(result.IsSuccess);
            Assert.True(result.ElapsedTime.TotalMilliseconds > 0);
            Assert.Null(result.OutputData);
        }

        [Fact]
        public async Task Run_ResultFalse_WhenPoolExhausted_IncludesRetryAfterSecondsFromAcquireTimeout()
        {
            // Arrange: a pool with its single page already checked out, and a short acquire
            // timeout so the next request exhausts the pool quickly and deterministically.
            using var objbuilder = new HtmlPdfBuilder(null);
            objbuilder.PagesBuffer(1);
            objbuilder.AcquireTimeout(20);
            await objbuilder.BuildAsync("Server");
            var heldPage = await objbuilder.AcquireAsync(CancellationToken.None);
            Assert.NotNull(heldPage);

            var requestHtmlPdf = await new RequestHtmlPdf<byte[]>("<h1>Test</h1>", "teste", new PdfPageConfig(), 5000).ToBytesCompress();

            // Act
            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, "Server")
                .Run(requestHtmlPdf, CancellationToken.None);

            // Assert: reported as a backpressure signal, with a retry hint derived from
            // AcquireTimeoutMs (20ms -> ceil to 1 second), not a bare "not available" failure.
            Assert.False(result.IsSuccess);
            Assert.True(result.BufferDrained);
            Assert.NotNull(result.Error);
            Assert.Equal(ErrorCode.PoolExhausted, result.Error!.Code);
            Assert.True(result.Error.Retryable);
            Assert.Equal(1, result.Error.RetryAfterSeconds);
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
        public async Task Run_ResultFalse_WhenUrlRejectedByDefaultPolicy()
        {
            // Arrange
            using var objbuilder = new HtmlPdfBuilder(null);
            await objbuilder.BuildAsync("Server");
            var requestHtmlPdf = await new RequestHtmlPdf<byte[]>("http://169.254.169.254/latest/meta-data/", "teste", new PdfPageConfig(), 5000, mode: RenderMode.Url).ToBytesCompress();

            // Act
            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, "Server")
                .Run(requestHtmlPdf, CancellationToken.None);

            // Assert: rejected fast by the policy, never attempted a real navigation.
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.Equal(ErrorCode.InvalidRequest, result.Error!.Code);
            Assert.True(result.ElapsedTime.TotalMilliseconds < 5000);
        }

        [Fact]
        public async Task Run_ResultFalse_WhenDecompressedRequestExceedsConfiguredLimit()
        {
            // Arrange: a tiny cap that any real request payload will exceed once decompressed.
            using var objbuilder = new HtmlPdfBuilder(null);
            objbuilder.MaxDecompressedRequestSize(10);
            var requestHtmlPdf = await new RequestHtmlPdf<byte[]>("<h1>Test</h1>", "teste", new PdfPageConfig(), 5000).ToBytesCompress();

            // Act
            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, "Server")
                .Run(requestHtmlPdf, CancellationToken.None);

            // Assert: rejected before the request is even parsed, no browser involved.
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.Equal(ErrorCode.InvalidRequest, result.Error!.Code);
            Assert.Contains("exceeds the configured limit", result.Error!.Message);
        }

        [Fact]
        public async Task ScopeRequest_ResultFalse_WhenDecompressedRequestExceedsConfiguredLimit()
        {
            // Arrange: same limit, exercised through HtmlPdfServerContext.Run instead of
            // HtmlPdfServer.Run - the other entry point that decompresses a client payload.
            using var objbuilder = new HtmlPdfBuilder(null);
            objbuilder.MaxDecompressedRequestSize(10);
            var requestHtmlPdf = await new RequestHtmlPdf<object>("<h1>Test</h1>", "teste", new PdfPageConfig(), 5000).ToBytesCompress();

            // Act
            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, "Server")
                .ScopeRequest(requestHtmlPdf)
                .Run(CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.Equal(ErrorCode.InvalidRequest, result.Error!.Code);
            Assert.Contains("exceeds the configured limit", result.Error!.Message);
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
