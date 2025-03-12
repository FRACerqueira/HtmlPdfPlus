// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace TestHtmlPdfPlus.HtmlPdfSrvPlus
{
    public class HostingExtensionsTest
    {

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
                cfg.AcquireWaitTime(100)
                   .PagesBuffer(1)
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
    }
}
