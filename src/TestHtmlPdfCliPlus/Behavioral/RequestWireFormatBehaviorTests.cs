// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using HtmlPdfPlus.Client.Core;

namespace TestHtmlPdfPlus.Behavioral
{
    /// <summary>
    /// Behavioral (given/when/then) regressions for the request wire format: the HTTP body is
    /// the request bytes (gzip-compressed JSON) sent directly as <c>application/octet-stream</c>
    /// - no base64/JSON-string wrapping layered on top purely because <c>byte[]</c> model binding
    /// defaults to that (see HtmlPdfEndpointExtensions.MapHtmlPdfEndpoints for the matching
    /// server-side change).
    /// </summary>
    public class RequestWireFormatBehaviorTests
    {
        [Fact]
        public async Task Given_HttpClientRun_When_RequestIsSent_Then_ContentTypeIsOctetStream()
        {
            // Given: a capturing handler that records the outgoing request without a real server.
            using var handler = new CapturingHandler();
            using var httpClient = new HttpClient(handler);

            // When: Run is awaited via the HttpClient overload.
            await HtmlPdfClient.Create("behavioral-request-format")
                .FromHtml("<html><body>hi</body></html>")
                .Run(httpClient, "http://localhost/GeneratePdf", CancellationToken.None);

            // Then: the content type is the raw octet stream, not JSON - so an ASP.NET `Accepts`
            // constraint on the mapped endpoint matches it, and no non-.NET client needs to know
            // to quote its base64 payload as a JSON string.
            Assert.NotNull(handler.CapturedRequest);
            Assert.Equal("application/octet-stream", handler.CapturedRequest!.Content!.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Given_HttpClientRun_When_RequestIsSent_Then_BodyIsTheRawCompressedBytesWithNoEnvelope()
        {
            // Given: a capturing handler that records the outgoing request without a real server.
            using var handler = new CapturingHandler();
            using var httpClient = new HttpClient(handler);

            // When: Run is awaited via the HttpClient overload.
            await HtmlPdfClient.Create("behavioral-request-format")
                .FromHtml("<html><body>hi</body></html>")
                .Run(httpClient, "http://localhost/GeneratePdf", CancellationToken.None);

            // Then: the captured body decompresses directly into the request - no base64 decode
            // or JSON-string unwrap step needed first, which is exactly what a non-.NET client
            // building the body by hand (gzip -> POST) would produce.
            Assert.NotNull(handler.CapturedBody);
            var request = RequestHtmlPdf<object>.FromBytesCompress(handler.CapturedBody!);
            Assert.Equal("behavioral-request-format", request.Alias);
            Assert.Equal(RenderMode.Html, request.Mode);
        }

        [Fact]
        public async Task Given_DisableCompress_When_RequestIsSent_Then_ContentTypeIsJsonAndBodyIsPlainReadableJson()
        {
            // Given: a client with compression disabled and a capturing handler.
            using var handler = new CapturingHandler();
            using var httpClient = new HttpClient(handler);
            var client = new HtmlPdfClientInstance("behavioral-request-format", DisableOptionsHtmlToPdf.DisableCompress);
            client.FromHtml("<html><body>hi</body></html>");

            // When: Run is awaited via the HttpClient overload.
            await client.Run(httpClient, "http://localhost/GeneratePdf", CancellationToken.None);

            // Then: the content type reflects what the bytes actually are (readable JSON, not
            // gzip) - and the body parses directly as JSON with no decompression step, exactly
            // the "curl a plain JSON file at the endpoint" workflow this option exists for.
            Assert.NotNull(handler.CapturedRequest);
            Assert.Equal("application/json", handler.CapturedRequest!.Content!.Headers.ContentType?.MediaType);
            Assert.NotNull(handler.CapturedBody);
            var request = RequestHtmlPdf<object>.FromBytes(handler.CapturedBody!);
            Assert.Equal("behavioral-request-format", request.Alias);
        }

        private sealed class CapturingHandler : HttpMessageHandler
        {
            public HttpRequestMessage? CapturedRequest { get; private set; }

            public byte[]? CapturedBody { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CapturedRequest = request;
                CapturedBody = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent([0x25, 0x50, 0x44, 0x46])
                };
            }
        }
    }
}
