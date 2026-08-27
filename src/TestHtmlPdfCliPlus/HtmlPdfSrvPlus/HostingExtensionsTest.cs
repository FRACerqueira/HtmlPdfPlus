// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using HtmlPdfPlus.Server.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace TestHtmlPdfPlus.HtmlPdfSrvPlus
{
    public class HostingExtensionsTest
    {
        /// <summary>
        /// A controllable stand-in for the host's real application lifetime: lets a test fire
        /// <c>ApplicationStopping</c> in isolation, without an actual host actually stopping (which
        /// would also drain in-flight requests first - the exact ordering this suite is checking).
        /// </summary>
        private sealed class FakeApplicationLifetime : IHostApplicationLifetime
        {
            private readonly CancellationTokenSource _stopping = new();
            private readonly CancellationTokenSource _stopped = new();
            public CancellationToken ApplicationStarted => CancellationToken.None;
            public CancellationToken ApplicationStopping => _stopping.Token;
            public CancellationToken ApplicationStopped => _stopped.Token;
            public void StopApplication() => _stopping.Cancel();

            /// <summary>
            /// Fires ApplicationStopped in isolation, simulating the point in the Generic Host
            /// stop sequence that comes AFTER the graceful drain completes - unlike
            /// ApplicationStopping, which fires at the start.
            /// </summary>
            public void CompleteStopping() => _stopped.Cancel();
        }

        [Fact]
        public void AddHtmlPdfServerPlus_RegistersSingletonServiceAndWarmup()
        {
            // Arrange
            var services = new ServiceCollection();
            // Act
            services.AddHtmlPdfService((cfg) => { }, "testAlias");
            var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetService<IHtmlPdfServer<object,byte[]>>();

            var mockHost = new Mock<IHost>();
            mockHost.Setup(h => h.Services).Returns(serviceProvider);
            var result = mockHost.Object.WarmupHtmlPdfService();

            // Assert
            Assert.True(result > TimeSpan.Zero);
        }


        [Fact]
        public void AddHtmlPdfServerPlus_RegistersSingletonServiceCustomAndWarmup()
        {
            // Arrange
            var services = new ServiceCollection();
            // Act
            services.AddHtmlPdfService<string,string>((cfg) => { }, "testAlias");
            var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetService<IHtmlPdfServer<string, string>>();

            var mockHost = new Mock<IHost>();
            mockHost.Setup(h => h.Services).Returns(serviceProvider);
            var result = mockHost.Object.WarmupHtmlPdfService<string, string>();

            // Assert
            Assert.True(result > TimeSpan.Zero);
        }

        [Fact]
        public void AddHtmlPdfServerPlus_RegistersSingletonServiceAndConfig()
        {
            // Arrange
            var services = new ServiceCollection();
            // Act
            services.AddHtmlPdfService((cfg) => 
            {
                cfg.PagesBuffer(1)
                   .Logger(Microsoft.Extensions.Logging.LogLevel.Trace, "teste")
                   .InitArguments("--disable-dev-shm-usage;-no-first-run");
            }, "testAlias");
            var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetService<IHtmlPdfServer<object, byte[]>>();

            var mockHost = new Mock<IHost>();
            mockHost.Setup(h => h.Services).Returns(serviceProvider);
            var result = mockHost.Object.WarmupHtmlPdfService();

            // Assert
            Assert.True(result > TimeSpan.Zero);
        }

        [Fact]
        public void WarmupHtmlPdfServerPlus_ThrowsException_WhenServiceNotRegistered()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var mockHost = new Mock<IHost>();
            mockHost.Setup(h => h.Services).Returns(serviceProvider);
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mockHost.Object.WarmupHtmlPdfService());
        }

        [Fact]
        public void AddHtmlPdfService_DoesNotDisposeBuilder_WhenOnlyApplicationStoppingFires()
        {
            // Given: a registered service and the host's real application-lifetime signal -
            // ApplicationStopping fires at the *start* of Generic Host shutdown, before the web
            // host's own hosted service (Kestrel) has drained requests already in flight.
            var services = new ServiceCollection();
            var lifetime = new FakeApplicationLifetime();
            services.AddSingleton<IHostApplicationLifetime>(lifetime);
            services.AddHtmlPdfService((cfg) => cfg.PagesBuffer(1), "testAlias");
            var serviceProvider = services.BuildServiceProvider();
            var service = (HtmlPdfServer<object, byte[]>)serviceProvider.GetRequiredService<IHtmlPdfServer<object, byte[]>>();

            // When: only ApplicationStopping fires (the host has not actually finished stopping).
            lifetime.StopApplication();

            // Then: the shared browser/pool must still be usable - tearing it down this early would
            // fail any request still in flight during the drain window instead of letting it finish.
            Assert.False(service.PdfSrvBuilder.IsDisposed);
        }

        [Fact]
        public void AddHtmlPdfService_DisposesBuilder_WhenApplicationStoppedFires()
        {
            // Given: a registered service and the host's real application-lifetime signal -
            // ApplicationStopped fires only after Generic Host's graceful drain completes, unlike
            // ApplicationStopping (see the test above). The ServiceProvider is deliberately never
            // disposed here, isolating this backstop from container disposal.
            var services = new ServiceCollection();
            var lifetime = new FakeApplicationLifetime();
            services.AddSingleton<IHostApplicationLifetime>(lifetime);
            services.AddHtmlPdfService((cfg) => cfg.PagesBuffer(1), "testAlias");
            var serviceProvider = services.BuildServiceProvider();
            var service = (HtmlPdfServer<object, byte[]>)serviceProvider.GetRequiredService<IHtmlPdfServer<object, byte[]>>();

            // When: ApplicationStopped fires, with no container disposal involved.
            lifetime.CompleteStopping();

            // Then: the backstop disposes the builder on its own - a hosting pattern that calls
            // StopAsync() without ever disposing the container must not leak the browser process.
            Assert.True(service.PdfSrvBuilder.IsDisposed);
        }

        [Fact]
        public void AddHtmlPdfService_DisposesBuilder_WhenServiceProviderIsDisposed()
        {
            // Given: a registered service, resolved so the DI container tracks the concrete
            // disposable instance the factory returned.
            var services = new ServiceCollection();
            services.AddHtmlPdfService((cfg) => cfg.PagesBuffer(1), "testAlias");
            var serviceProvider = services.BuildServiceProvider();
            var service = (HtmlPdfServer<object, byte[]>)serviceProvider.GetRequiredService<IHtmlPdfServer<object, byte[]>>();

            // When: the container itself is disposed - the point in the Generic Host lifecycle
            // (host.Dispose(), called after StopAsync/graceful drain) that should own teardown now.
            serviceProvider.Dispose();

            // Then: disposal still happens - removing the premature ApplicationStopping hook must
            // not silently leak the browser/Playwright connection.
            Assert.True(service.PdfSrvBuilder.IsDisposed);
        }

        [Fact]
        public void AddHtmlPdfService_EnablesLoggingByDefault_EvenWhenConfigCallbackDoesNotCallLogger()
        {
            // Given: a config callback that customizes something unrelated (buffer size) and
            // never itself calls .Logger(...) - a real ILoggerFactory is registered so a Logger()
            // call, if it happens, actually produces a non-null Log.
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddHtmlPdfService((cfg) => cfg.PagesBuffer(1), "testAlias");
            var serviceProvider = services.BuildServiceProvider();
            var service = (HtmlPdfServer<object, byte[]>)serviceProvider.GetRequiredService<IHtmlPdfServer<object, byte[]>>();

            // Then: logging is enabled the same way it would be for AddHtmlPdfService() with no
            // config at all - a config callback that only touches unrelated settings must not
            // silently opt this registration out of logging.
            Assert.NotNull(service.PdfSrvBuilder.Log);
        }

        [Fact]
        public void AddHtmlPdfService_GeneratesDistinctSourceAlias_ForDifferentRegistrations_WhenNoAliasOrLoggerGiven()
        {
            // Given: two registrations that supply neither an explicit sourceAlias nor a .Logger()
            // call inside their config callback - the two conditions that previously left
            // sourcealias empty for both, making every metric they report indistinguishable.
            var services = new ServiceCollection();
            services.AddHtmlPdfService<string, string>((cfg) => cfg.PagesBuffer(1));
            services.AddHtmlPdfService<int, byte[]>((cfg) => cfg.PagesBuffer(1));
            var serviceProvider = services.BuildServiceProvider();
            var service1 = (HtmlPdfServer<string, string>)serviceProvider.GetRequiredService<IHtmlPdfServer<string, string>>();
            var service2 = (HtmlPdfServer<int, byte[]>)serviceProvider.GetRequiredService<IHtmlPdfServer<int, byte[]>>();

            // Then: each got its own generated, non-empty alias instead of both collapsing to "".
            Assert.NotEmpty(service1.SourceAlias);
            Assert.NotEmpty(service2.SourceAlias);
            Assert.NotEqual(service1.SourceAlias, service2.SourceAlias);
        }

        [Fact]
        public void AddHtmlPdfService_CreatesDefaultLogger_WithFinalResolvedAliasAsCategory()
        {
            // Given: no explicit sourceAlias and no .Logger() call in the config callback, so
            // sourceAlias only reaches its final value (the guid fallback) AFTER config?.Invoke
            // runs - the default logger must be created using that final value as its category,
            // not the still-empty parameter it started as.
            string? capturedCategory = null;
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory
                .Setup(f => f.CreateLogger(It.IsAny<string>()))
                .Callback<string>(name => capturedCategory = name)
                .Returns(NullLogger.Instance);

            var services = new ServiceCollection();
            services.AddSingleton(mockLoggerFactory.Object);
            services.AddHtmlPdfService((cfg) => cfg.PagesBuffer(1));
            var serviceProvider = services.BuildServiceProvider();
            var service = (HtmlPdfServer<object, byte[]>)serviceProvider.GetRequiredService<IHtmlPdfServer<object, byte[]>>();

            // Then: the logger's category matches the final resolved sourceAlias - previously it
            // was created with "" before the guid fallback was ever computed, so a category-scoped
            // Logging:LogLevel override could never target this instance.
            Assert.False(string.IsNullOrEmpty(capturedCategory));
            Assert.Equal(service.SourceAlias, capturedCategory);
        }
    }
}
