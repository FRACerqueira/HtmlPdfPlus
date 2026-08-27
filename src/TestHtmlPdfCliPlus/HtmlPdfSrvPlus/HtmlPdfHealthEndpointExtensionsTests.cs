// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Net;
using System.Net.Http.Json;
using HtmlPdfPlus;
using HtmlPdfPlus.Server.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestHtmlPdfPlus.HtmlPdfSrvPlus
{
    /// <summary>
    /// Exercises <c>MapHtmlPdfHealthEndpoints</c> against a real <see cref="HtmlPdfBuilder"/>,
    /// so the liveness/readiness HTTP shaping is verified end-to-end without needing a real
    /// orchestrator.
    /// </summary>
    public class HtmlPdfHealthEndpointExtensionsTests
    {
        [Fact]
        public void HtmlPdfHealthStatus_IsUnhealthy_WhenBrowserNotConnected()
        {
            var status = new HtmlPdfHealthStatus(BrowserConnected: false, Recovering: false, AvailablePages: 3);
            Assert.False(status.Healthy);
        }

        [Fact]
        public void HtmlPdfHealthStatus_IsUnhealthy_WhenRecovering()
        {
            var status = new HtmlPdfHealthStatus(BrowserConnected: true, Recovering: true, AvailablePages: 0);
            Assert.False(status.Healthy);
        }

        [Fact]
        public void HtmlPdfHealthStatus_IsHealthy_WhenConnectedAndNotRecovering_EvenIfPoolIsMomentarilyEmpty()
        {
            // A saturated pool (AvailablePages == 0) is still healthy - saturation is
            // per-request backpressure (PoolExhausted/Retry-After), not a readiness concern.
            var status = new HtmlPdfHealthStatus(BrowserConnected: true, Recovering: false, AvailablePages: 0);
            Assert.True(status.Healthy);
        }

        [Fact]
        public void HtmlPdfHealthStatus_IsUnhealthy_WhenPoolStarved_EvenIfConnectedAndNotRecovering()
        {
            // Unlike a momentarily saturated pool (AvailablePages == 0, still healthy - see the
            // test above), a pool that came back from recovery with zero pages cannot
            // self-correct on its own: no request can ever acquire a page in the first place to
            // later return one, so it must not be reported as ready.
            var status = new HtmlPdfHealthStatus(BrowserConnected: true, Recovering: false, AvailablePages: 0, PoolStarved: true);
            Assert.False(status.Healthy);
        }

        [Fact]
        public async Task Given_HealthyBuilder_When_LiveAndReadyEndpointsCalled_Then_BothReturn200()
        {
            // Given: a real, freshly built browser/pool.
            using var builder = new HtmlPdfBuilder(null);
            var server = await builder.BuildAsync("Server");
            using var host = await CreateTestHost(server);
            using var client = host.GetTestClient();

            // When
            using var liveResponse = await client.GetAsync("/healthz", TestContext.Current.CancellationToken);
            using var readyResponse = await client.GetAsync("/readyz", TestContext.Current.CancellationToken);

            // Then
            Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
            var status = await readyResponse.Content.ReadFromJsonAsync<HtmlPdfHealthStatus>(TestContext.Current.CancellationToken);
            Assert.NotNull(status);
            Assert.True(status!.Healthy);
            Assert.True(status.BrowserConnected);
            Assert.False(status.Recovering);
        }

        [Fact]
        public async Task Given_UnexpectedDisconnect_When_HealthCapturedAtDetectionTime_Then_ReportsUnhealthy()
        {
            // Given: a real browser/pool. CloseAsync raises the same Disconnected event
            // Playwright fires on a real crash. Recovery kicks off from that same event
            // (HtmlPdfBuilder's own handler runs first, since it subscribed first), so how
            // it races a *subsequent* HTTP round-trip is environment-dependent - it was
            // observed to complete before an awaited-afterwards check on some CI runners
            // and not others, regardless of OS. To assert the actual guarantee (health
            // state reflects the disconnect at detection time) without racing that
            // background recovery, the health snapshot is captured synchronously inside
            // the Disconnected handler itself - the same call stack that flips
            // HtmlPdfBuilder's internal recovering flag to true, before any scheduling gap
            // gives recovery a chance to run further.
            using var builder = new HtmlPdfBuilder(null);
            builder.PagesBuffer(1);
            var server = (HtmlPdfServer<object, byte[]>)await builder.BuildAsync("Server");

            var browser = builder.CurrentBrowser!;
            HtmlPdfHealthStatus? capturedStatus = null;
            var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            browser.Disconnected += (_, _) =>
            {
                capturedStatus = server.GetHealthStatus();
                disconnected.TrySetResult();
            };

            // When
            await browser.CloseAsync();
            await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            // Then: an orchestrator polling /readyz must see this instance drop out of
            // rotation the moment the disconnect is detected, not only once recovery
            // finishes relaunching Chromium.
            Assert.NotNull(capturedStatus);
            Assert.True(capturedStatus!.Recovering);
            Assert.False(capturedStatus.Healthy);
        }

        [Fact]
        public async Task Given_ServerWithoutHealthSignal_When_ReadyEndpointCalled_Then_Returns503()
        {
            // Given: a registration that isn't the library's own concrete IHtmlPdfServer (a
            // custom decorator, a test double) - it has no browser/pool to report on, which
            // MapHtmlPdfHealthEndpoints treats as a readiness failure rather than a crash.
            using var host = await CreateTestHost(new NoHealthSignalServer());
            using var client = host.GetTestClient();

            // When
            using var readyResponse = await client.GetAsync("/readyz", TestContext.Current.CancellationToken);

            // Then
            Assert.Equal(HttpStatusCode.ServiceUnavailable, readyResponse.StatusCode);
            var status = await readyResponse.Content.ReadFromJsonAsync<HtmlPdfHealthStatus>(TestContext.Current.CancellationToken);
            Assert.NotNull(status);
            Assert.False(status!.Healthy);
        }

        private sealed class NoHealthSignalServer : IHtmlPdfServer<object, byte[]>
        {
            public void Dispose()
            {
            }

            public IHtmlPdfServerContext<object, byte[]> ScopeData(object? inputparam = default) => throw new NotSupportedException();

            public IHtmlPdfServerContext<object, byte[]> ScopeRequest(byte[] requestClient) => throw new NotSupportedException();

            public Task<HtmlPdfResult<byte[]>> Run(byte[] requestClient, CancellationToken token = default) => throw new NotSupportedException();
        }

        private static async Task<IHost> CreateTestHost(IHtmlPdfServer<object, byte[]> server)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddSingleton(server);
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapHtmlPdfHealthEndpoints());
                    });
                })
                .Build();
            await host.StartAsync();
            return host;
        }
    }
}
