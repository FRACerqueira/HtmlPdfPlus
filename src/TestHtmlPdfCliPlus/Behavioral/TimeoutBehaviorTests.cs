// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using HtmlPdfPlus.Server.Core;

namespace TestHtmlPdfPlus.Behavioral
{
    /// <summary>
    /// Behavioral (given/when/then) regressions for the Fase 0 reliability fixes.
    /// These exercise the public API end-to-end instead of internal members.
    /// </summary>
    public class TimeoutBehaviorTests
    {
        [Fact]
        public async Task Given_SelfCancelingSubmitDelegate_When_Run_Then_ReturnsTimeoutResultWithoutThrowing()
        {
            // Given: a caller-supplied submit delegate whose own transport raises cancellation
            // (e.g. a gRPC deadline, a TCP abort, an internal token) independently of the
            // client's timeout race.
            var client = HtmlPdfClient.Create("behavioral-timeout")
                .FromHtml("<html><body>hi</body></html>")
                .Timeout(5000);

            // When: Run is awaited.
            HtmlPdfResult<byte[]>? result = null;
            var ex = await Record.ExceptionAsync(async () =>
            {
                result = await client.Run(async (bytes, ct) =>
                {
                    await Task.Yield();
                    throw new OperationCanceledException(ct);
                }, CancellationToken.None);
            });

            // Then: no exception escapes the Result pattern, and the failure is reported,
            // not swallowed into a null result.
            Assert.Null(ex);
            Assert.NotNull(result);
            Assert.False(result!.IsSuccess);
        }

        [Fact]
        public async Task Given_SelfCancelingBeforePdfHook_When_Run_Then_ReturnsFailureResultInsteadOfStaleSuccess()
        {
            // Given: a real page pool (a real Chromium is what would actually let the buggy
            // fall-through generate a PDF from the stale HTML and report a false success -
            // an unbuilt pool masks the bug behind an unrelated "no page available" failure)
            // and a BeforePDF hook whose own logic raises cancellation before it finishes
            // rewriting the HTML (e.g. it delegates to another cancellable call).
            using var builder = new HtmlPdfBuilder(null);
            await builder.BuildAsync("Server");
            var requestHtmlPdf = await new RequestHtmlPdf<object>("<h1>Original</h1>", "teste", new PdfPageConfig(), 5000).ToBytesCompress();

            // When: Run is awaited.
            HtmlPdfResult<byte[]>? result = null;
            var ex = await Record.ExceptionAsync(async () =>
            {
                result = await new HtmlPdfServer<object, byte[]>(builder, "Server")
                    .ScopeRequest(requestHtmlPdf)
                    .BeforePDF(async (html, _, ct) =>
                    {
                        await Task.Yield();
                        throw new OperationCanceledException(ct);
                    })
                    .Run(CancellationToken.None);
            });

            // Then: no exception escapes the Result pattern, and the hook's cancellation is
            // reported as a failure - it must not fall through and silently generate a PDF
            // from the original, un-rewritten HTML as if BeforePDF had succeeded.
            Assert.Null(ex);
            Assert.NotNull(result);
            Assert.False(result!.IsSuccess);
            Assert.IsType<OperationCanceledException>(result!.Error);
        }
    }
}
