// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using HtmlPdfPlus.Server.Core;
using Microsoft.Playwright;

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
        public async Task Run_ResultFalse_WhenOverallDeadlineElapsesWhileWaitingForPool_ReportsTimeoutNotPoolExhausted()
        {
            // Arrange: pool with its single page checked out, and a request-level Timeout far
            // shorter than the pool's default AcquireTimeoutMs (5000ms, left unconfigured here)
            // - the overall deadline governs the outcome, not the pool's own acquire window,
            // so this must not be misreported as PoolExhausted.
            using var objbuilder = new HtmlPdfBuilder(null);
            objbuilder.PagesBuffer(1);
            await objbuilder.BuildAsync("Server");
            var heldPage = await objbuilder.AcquireAsync(CancellationToken.None);
            Assert.NotNull(heldPage);

            var requestHtmlPdf = await new RequestHtmlPdf<byte[]>("<h1>Test</h1>", "teste", new PdfPageConfig(), 200).ToBytesCompress();

            // Act
            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, "Server")
                .Run(requestHtmlPdf, CancellationToken.None);

            // Assert: the overall deadline (200ms) elapsed while waiting for a page that
            // never freed up - classified as Timeout, distinct from a genuine pool-exhaustion
            // event where the pool's own configured window is what elapses first.
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.Equal(ErrorCode.Timeout, result.Error!.Code);
            Assert.True(result.ElapsedTime.TotalMilliseconds < 5000);
        }

        [Fact]
        public async Task Run_ResultFalse_WhenDeadlineAlreadyExpiredInTransit_ReturnsImmediateTimeoutWithoutAttemptingRender()
        {
            // Arrange: SentAtUtc says the request left the client 2s ago, but Timeout only
            // allows 500ms total - transit alone already exhausted the budget. The pool is
            // never even built, so any interaction with it would throw - proving the server
            // fails fast on the already-expired deadline instead of attempting to render.
            using var objbuilder = new HtmlPdfBuilder(null);
            var sentAtUtc = DateTimeOffset.UtcNow.AddSeconds(-2);
            var requestHtmlPdf = await new RequestHtmlPdf<object>("<h1>Test</h1>", "teste", new PdfPageConfig(), 500, sentAtUtc: sentAtUtc).ToBytesCompress();

            // Act
            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, "Server")
                .Run(requestHtmlPdf, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.Equal(ErrorCode.Timeout, result.Error!.Code);
            Assert.True(result.ElapsedTime.TotalMilliseconds < 500);
        }

        [Fact]
        public async Task Run_ResultTrue_WhenSentAtUtcImplausiblyOld_TreatsItAsClockSkewAndIgnoresIt()
        {
            // Arrange: SentAtUtc claims the request is 30s old - far beyond MaxPlausibleTransitMs
            // (5000ms) - so this must be treated as clock skew between client and server rather
            // than a genuinely near-exhausted deadline, or a skewed server clock would fail
            // every request outright regardless of actual elapsed time.
            using var objbuilder = new HtmlPdfBuilder(null);
            await objbuilder.BuildAsync("Server");
            var sentAtUtc = DateTimeOffset.UtcNow.AddSeconds(-30);
            var requestHtmlPdf = await new RequestHtmlPdf<byte[]>("<h1>Test</h1>", "teste", new PdfPageConfig(), 5000, sentAtUtc: sentAtUtc).ToBytesCompress();

            // Act
            var result = await new HtmlPdfServer<object, byte[]>(objbuilder, "Server")
                .Run(requestHtmlPdf, CancellationToken.None);

            // Assert: succeeds normally - the implausible transit was ignored instead of being
            // treated as an already-exhausted deadline.
            Assert.True(result.IsSuccess);
            Assert.Null(result.Error);
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

        [Fact]
        public void ClassifyGeneratePdfException_PlaywrightException_ReturnsRetryableRenderFailed()
        {
            // Arrange
            var ex = new PlaywrightException("Target page, context or browser has been closed");
            // Act
            var error = HtmlPdfServer<object, byte[]>.ClassifyGeneratePdfException(ex);
            // Assert
            Assert.Equal(ErrorCode.RenderFailed, error.Code);
            Assert.True(error.Retryable);
        }

        [Fact]
        public void ClassifyGeneratePdfException_NonPlaywrightException_FallsBackToGenericClassification()
        {
            // Arrange
            var ex = new InvalidOperationException("boom");
            // Act
            var error = HtmlPdfServer<object, byte[]>.ClassifyGeneratePdfException(ex);
            // Assert - unchanged behavior for every failure kind that isn't a live browser/render failure.
            Assert.Equal(ErrorCode.InvalidRequest, error.Code);
            Assert.False(error.Retryable);
        }
    }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
}
