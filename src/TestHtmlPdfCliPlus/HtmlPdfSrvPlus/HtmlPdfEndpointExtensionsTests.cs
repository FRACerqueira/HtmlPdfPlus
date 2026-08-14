// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Net;
using System.Net.Http.Json;
using HtmlPdfPlus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestHtmlPdfPlus.HtmlPdfSrvPlus
{
    /// <summary>
    /// Exercises <c>MapHtmlPdfEndpoints</c> against a fake <see cref="IHtmlPdfServer{TIn, TOut}"/>,
    /// so the HTTP response shaping (raw bytes vs JSON, status code mapping) is verified without
    /// needing a real Chromium render.
    /// </summary>
    public class HtmlPdfEndpointExtensionsTests
    {
        [Fact]
        public async Task Given_SuccessfulByteArrayResult_When_EndpointIsCalled_Then_ResponseIsRawPdfBytes()
        {
            // Given: a fake server that reports a successful byte[] conversion.
            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            var result = new HtmlPdfResult<byte[]>(true, false, TimeSpan.Zero, pdfBytes, null);
            using var host = await CreateTestHost<object, byte[]>(new FakeHtmlPdfServer<object, byte[]>(result));

            // When: the mapped endpoint is invoked.
            using var client = host.GetTestClient();
            using var response = await client.PostAsJsonAsync("/GeneratePdf", new byte[] { 1, 2, 3 }, TestContext.Current.CancellationToken);

            // Then: the body is the raw PDF bytes, served as application/pdf - no JSON envelope.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(pdfBytes, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Given_FailedResult_When_EndpointIsCalled_Then_ResponseIsErrorInfoWithMappedStatus()
        {
            // Given: a fake server that reports a failure classified as InvalidRequest.
            var error = new ErrorInfo(ErrorCode.InvalidRequest, "bad request", retryable: false);
            var result = new HtmlPdfResult<byte[]>(false, false, TimeSpan.Zero, default, error);
            using var host = await CreateTestHost<object, byte[]>(new FakeHtmlPdfServer<object, byte[]>(result));

            // When: the mapped endpoint is invoked.
            using var client = host.GetTestClient();
            using var response = await client.PostAsJsonAsync("/GeneratePdf", new byte[] { 1, 2, 3 }, TestContext.Current.CancellationToken);

            // Then: the status line itself carries the failure (InvalidRequest -> 400), and the
            // body is the exact structured ErrorInfo, not an embedded IsSuccess:false on a 200.
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ErrorInfo>(TestContext.Current.CancellationToken);
            Assert.NotNull(body);
            Assert.Equal(ErrorCode.InvalidRequest, body!.Code);
            Assert.Equal("bad request", body.Message);
        }

        [Fact]
        public async Task Given_NonByteArrayOutput_When_EndpointIsCalled_Then_ResponseIsJson()
        {
            // Given: a fake server whose output type is a small string (e.g. a saved filename),
            // not the PDF itself - the D5 binary-response change does not apply here.
            var result = new HtmlPdfResult<string>(true, false, TimeSpan.Zero, "file.pdf", null);
            using var host = await CreateTestHost<object, string>(new FakeHtmlPdfServer<object, string>(result));

            // When: the mapped endpoint is invoked.
            using var client = host.GetTestClient();
            using var response = await client.PostAsJsonAsync("/GeneratePdf", new byte[] { 1, 2, 3 }, TestContext.Current.CancellationToken);

            // Then: the response is JSON carrying the full HtmlPdfResult<string>, as before D5.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<HtmlPdfResult<string>>(TestContext.Current.CancellationToken);
            Assert.NotNull(body);
            Assert.True(body!.IsSuccess);
            Assert.Equal("file.pdf", body.OutputData);
        }

        private static async Task<IHost> CreateTestHost<TIn, TOut>(IHtmlPdfServer<TIn, TOut> fake)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddSingleton(fake);
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapHtmlPdfEndpoints<TIn, TOut>("/GeneratePdf"));
                    });
                })
                .Build();
            await host.StartAsync();
            return host;
        }

        private sealed class FakeHtmlPdfServer<TIn, TOut>(HtmlPdfResult<TOut> result) : IHtmlPdfServer<TIn, TOut>
        {
            public IHtmlPdfServerContext<TIn, TOut> ScopeData(TIn? inputparam = default) => throw new NotSupportedException();

            public IHtmlPdfServerContext<TIn, TOut> ScopeRequest(byte[] requestClient) => new FakeContext(result);

            public Task<HtmlPdfResult<TOut>> Run(byte[] requestClient, CancellationToken token = default) => Task.FromResult(result);

            public void Dispose()
            {
            }

            private sealed class FakeContext(HtmlPdfResult<TOut> result) : IHtmlPdfServerContext<TIn, TOut>
            {
                public IHtmlPdfServerContext<TIn, TOut> FromHtml(string html, int converttimeout = 30000, bool minify = true) => this;

                public IHtmlPdfServerContext<TIn, TOut> FromUrl(Uri value, int converttimeout = 30000) => this;

                public IHtmlPdfServerContext<TIn, TOut> FromRazor<T>(string templatetext, T model, int converttimeout = 30000, bool minify = true) => this;

                public IHtmlPdfServerContext<TIn, TOut> BeforePDF(Func<string, TIn?, CancellationToken, Task<string>> inputParam) => this;

                public IHtmlPdfServerContext<TIn, TOut> AfterPDF(Func<byte[]?, TIn?, CancellationToken, Task<TOut>> outputParam) => this;

                public Task<HtmlPdfResult<TOut>> Run(CancellationToken token = default) => Task.FromResult(result);
            }
        }
    }
}
