// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Diagnostics;
using System.Net;
using System.Text.Json;
using HtmlPdfPlus;
using HtmlPdfPlus.Shared.Core;

namespace TestHtmlPdfPlus.Behavioral
{
    /// <summary>
    /// Behavioral (given/when/then) regression proving that <c>.Timeout()</c> is enforced
    /// locally on the <see cref="HttpClient"/> submit path, not just forwarded to the server
    /// inside the request body.
    /// </summary>
    public class HttpClientTimeoutBehaviorTests
    {
        [Fact]
        public async Task Given_HttpClientPath_When_TimeoutConfigured_Then_RequestIsCanceledLocallyOnDeadline()
        {
            // Given: a server that takes far longer to respond than the configured .Timeout(),
            // reached only through HttpClient (the exact path used in the README's main example).
            var serverDelay = TimeSpan.FromMilliseconds(1500);
            var configuredTimeout = 100;
            using var handler = new SlowHandler(serverDelay);
            using var httpClient = new HttpClient(handler);

            var sw = Stopwatch.StartNew();

            // When: Run is awaited via the HttpClient overload.
            var result = await HtmlPdfClient.Create("behavioral-httpclient-timeout")
                .FromHtml("<html><body>hi</body></html>")
                .Timeout(configuredTimeout)
                .Run(httpClient, "http://localhost/GeneratePdf", TestContext.Current.CancellationToken);

            sw.Stop();

            // Then: the call returns close to the configured timeout, not after waiting out the
            // full server delay - proving the deadline is enforced on this side of the wire, not
            // only forwarded to the server inside the request body.
            Assert.False(result.IsSuccess);
            Assert.True(sw.Elapsed < serverDelay, $"Expected to return well before the {serverDelay} server delay, took {sw.Elapsed}.");
        }

        private sealed class SlowHandler(TimeSpan delay) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(delay, cancellationToken);
                // The client expects OutputData to be GZip-compressed by default (DisableCompress
                // is not set), matching what the real server would send back.
                var compressed = await GZipHelper.CompressAsync([1, 2, 3], cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new HtmlPdfResult<byte[]>(true, false, TimeSpan.Zero, compressed)))
                };
            }
        }
    }
}
