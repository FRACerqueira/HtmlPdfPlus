// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HtmlPdfPlus;

namespace TestHtmlPdfPlus.Behavioral
{
    /// <summary>
    /// Behavioral (given/when/then) regressions for the D5 response format: a byte[] output is
    /// served as the raw PDF body (no JSON envelope, no base64), and a non-2xx response is
    /// expected to carry the structured <see cref="ErrorInfo"/> contract in its body.
    /// </summary>
    public class BinaryResponseBehaviorTests
    {
        [Fact]
        public async Task Given_SuccessfulResponse_When_OutputIsBytes_Then_BodyIsReadAsRawPdfBytes()
        {
            // Given: a server that returns the PDF bytes directly, with an application/pdf
            // content type - no JSON envelope, no base64 - exactly what a host implementing the
            // new contract would send.
            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37 }; // "%PDF-1.7"
            using var handler = new RawBytesHandler(pdfBytes, "application/pdf");
            using var httpClient = new HttpClient(handler);

            // When: Run is awaited via the HttpClient overload.
            var result = await HtmlPdfClient.Create("behavioral-binary-response")
                .FromHtml("<html><body>hi</body></html>")
                .Run(httpClient, "http://localhost/GeneratePdf", CancellationToken.None);

            // Then: the raw body bytes surface unchanged as OutputData, with no JSON parsing step
            // in between that could have expected (and failed to find) an envelope.
            Assert.True(result.IsSuccess);
            Assert.Null(result.Error);
            Assert.Equal(pdfBytes, result.OutputData);
        }

        [Fact]
        public async Task Given_FailureResponse_When_BodyIsErrorInfo_Then_ResultCarriesTheSameErrorCodeAndMessage()
        {
            // Given: a server that rejects the request and reports it via a non-2xx status line
            // plus a structured ErrorInfo body - not a 200 with an embedded IsSuccess:false.
            var error = new ErrorInfo(ErrorCode.InvalidRequest, "The URL was rejected by the configured URL policy", retryable: false);
            using var handler = new JsonBodyHandler(HttpStatusCode.BadRequest, JsonSerializer.Serialize(error));
            using var httpClient = new HttpClient(handler);

            // When: Run is awaited via the HttpClient overload.
            var result = await HtmlPdfClient.Create("behavioral-binary-response")
                .FromHtml("<html><body>hi</body></html>")
                .Run(httpClient, "http://localhost/GeneratePdf", CancellationToken.None);

            // Then: the client surfaces the exact ErrorInfo the server sent, instead of a generic
            // "status code + reason phrase" error that discards the real classification.
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.Equal(ErrorCode.InvalidRequest, result.Error!.Code);
            Assert.Equal("The URL was rejected by the configured URL policy", result.Error.Message);
        }

        [Fact]
        public async Task Given_FailureResponse_When_BodyIsNotErrorInfo_Then_FallsBackToAGenericErrorInsteadOfThrowing()
        {
            // Given: a non-2xx response whose body isn't the ErrorInfo contract at all (e.g. an
            // upstream proxy's own HTML error page, or a host that hasn't adopted the contract).
            using var handler = new JsonBodyHandler(HttpStatusCode.InternalServerError, "<html><body>502 Bad Gateway</body></html>");
            using var httpClient = new HttpClient(handler);

            // When: Run is awaited via the HttpClient overload.
            var result = await HtmlPdfClient.Create("behavioral-binary-response")
                .FromHtml("<html><body>hi</body></html>")
                .Run(httpClient, "http://localhost/GeneratePdf", CancellationToken.None);

            // Then: parsing the malformed body never throws out of Run - it degrades to a
            // generic error built from the status line instead.
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
        }

        private sealed class RawBytesHandler(byte[] body, string contentType) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(body)
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                return Task.FromResult(response);
            }
        }

        private sealed class JsonBodyHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }
        }
    }
}
