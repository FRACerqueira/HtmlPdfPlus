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
        public async Task Given_DisconnectedBrowser_When_ReadyEndpointCalled_Then_Returns503BeforeRecoveryCompletes()
        {
            // Given: a real browser/pool that has just disconnected. CloseAsync raises the same
            // Disconnected event Playwright fires on a real crash (same simulation used
            // elsewhere in this suite). The Disconnected event and IBrowser.IsConnected flip
            // together, but the event delivery itself races CloseAsync's returned task across
            // platforms (the notification travels through Playwright's driver process), so we
            // await the event directly instead of assuming CloseAsync's completion means the
            // health endpoint already sees the disconnect.
            using var builder = new HtmlPdfBuilder(null);
            builder.PagesBuffer(1);
            var server = await builder.BuildAsync("Server");
            using var host = await CreateTestHost(server);
            using var client = host.GetTestClient();

            var browser = builder.CurrentBrowser!;
            var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            browser.Disconnected += (_, _) => disconnected.TrySetResult();

            // When
            await browser.CloseAsync();
            await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            using var readyResponse = await client.GetAsync("/readyz", TestContext.Current.CancellationToken);

            // Then: readiness reflects the disconnect immediately - an orchestrator polling
            // /readyz must see this instance drop out of rotation while it recovers.
            Assert.Equal(HttpStatusCode.ServiceUnavailable, readyResponse.StatusCode);
            var status = await readyResponse.Content.ReadFromJsonAsync<HtmlPdfHealthStatus>(TestContext.Current.CancellationToken);
            Assert.NotNull(status);
            Assert.False(status!.Healthy);
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
