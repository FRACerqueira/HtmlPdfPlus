// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using HtmlPdfPlus.Server.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace TestHtmlPdfPlus.HtmlPdfSrvPlus
{
#pragma warning disable IDE0079
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
    public class HtmlPdfBuilderTest
    {

        [Fact]
        public void Ensure_Create_Error_When_InvalidPageBuffer()
        {
            IHtmlPdfSrvBuilder obj = new HtmlPdfBuilder();
            Assert.Throws<ArgumentException>(() =>
            {

                obj.PagesBuffer(0);
            });
            ((IDisposable)obj).Dispose();
        }

        [Theory]
        [InlineData(9)]
        [InlineData(501)]
        public void Ensure_Create_Error_When_InvalidAcquireWaitTime(int wait)
        {

            IHtmlPdfSrvBuilder obj = new HtmlPdfBuilder();
            Assert.Throws<ArgumentException>(() =>           
            {
                obj.AcquireWaitTime(wait);
            });
            ((IDisposable)obj).Dispose();
        }

        [Theory]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        public void Ensure_Run_Error_When_InvalidLogLevel(LogLevel level)
        {
            var loggerfact = NullLoggerFactory.Instance;

            IHtmlPdfSrvBuilder obj = new HtmlPdfBuilder(loggerfact);
            Assert.Throws<ArgumentException>(() =>
            {
                obj.Logger(level);
            });
            ((IDisposable)obj).Dispose();
        }

        [Fact]
        public async Task Ensure_buid_With_DefaultBuffer()
        {
            using var obj = new HtmlPdfBuilder();
            await obj.BuildAsync("Teste");
            Assert.Equal(5, obj.BufferLength);
        }

        [Fact]
        public async Task Ensure_buid_With_CustomBuffer()
        {
            using var obj = new HtmlPdfBuilder();
            obj.InitArguments("--disable-dev-shm-usage;-no-first-run");
            obj.PagesBuffer(1);
            await obj.BuildAsync("Teste");
            Assert.Equal(1, obj.BufferLength);
        }


        [Fact]
        public async Task Ensure_buid_With_AccquireBuffer()
        {
            using var obj = new HtmlPdfBuilder(null);
            using var cts = new CancellationTokenSource();
            await obj.BuildAsync("Teste");
            cts.CancelAfter(100);
            obj.Acquire(cts.Token);
            Assert.Equal(4, obj.BufferLength);
        }

        [Fact]
        public async Task Ensure_buid_With_AccquireTimeout()
        {
            using var obj = new HtmlPdfBuilder();
            obj.AcquireWaitTime(10);
            obj.AcquireTimeout(20);
            using var cts = new CancellationTokenSource();
            obj.PagesBuffer(1);
            await obj.BuildAsync("Teste");
            cts.CancelAfter(200);
            var firtpage = obj.Acquire(cts.Token);
            var page = obj.Acquire(CancellationToken.None);
            Assert.NotNull(firtpage);
            Assert.Null(page);
        }

        [Fact]
        public async Task Ensure_buid_With_NotBufferExternalTimeout()
        {
            using var obj = new HtmlPdfBuilder();
            using var cts = new CancellationTokenSource();
            obj.PagesBuffer(1);
            await obj.BuildAsync("Teste");
            cts.CancelAfter(200);
            var firtpage = obj.Acquire(cts.Token);
            var page = obj.Acquire(cts.Token);
            Assert.NotNull(firtpage);
            Assert.Null(page);
        }


        [Fact]
        public async Task Ensure_buid_With_RestoreAvailableBuffer()
        {
            using var obj = new HtmlPdfBuilder();
            using var cts = new CancellationTokenSource();
            await obj.BuildAsync("Teste");
            cts.CancelAfter(100);
            var page = obj.Acquire(cts.Token);
            if (page is not null)
            {
                await obj.RestoreAvailableBuffer(page);
            }
            Assert.Equal(5, obj.BufferLength);
        }

        [Fact]
        public async Task Ensure_Browser_AutoRecovers_When_Disconnected()
        {
            // Given: a built pool and a reference to the live browser instance.
            using var obj = new HtmlPdfBuilder();
            obj.PagesBuffer(2);
            await obj.BuildAsync("Teste");
            var deadBrowser = obj.CurrentBrowser;

            // When: the Chromium process disconnects unexpectedly. CloseAsync raises the same
            // Disconnected event Playwright fires on a real crash, so it is a faithful simulation.
            await deadBrowser!.CloseAsync();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (obj.CurrentBrowser is null || !obj.CurrentBrowser.IsConnected || obj.BufferLength < 2)
            {
                cts.Token.ThrowIfCancellationRequested();
                await Task.Delay(50, cts.Token);
            }

            // Then: recovery replaced the dead browser and refilled the pool with usable pages,
            // without any manual restart of the builder.
            Assert.NotSame(deadBrowser, obj.CurrentBrowser);
            Assert.Equal(2, obj.BufferLength);
            var page = obj.Acquire(cts.Token);
            Assert.NotNull(page);
        }

        [Fact]
        public async Task Ensure_Browser_AutoRecovers_From_Consecutive_Disconnects()
        {
            // Given: a builder that already recovered once from a crash.
            using var obj = new HtmlPdfBuilder();
            obj.PagesBuffer(2);
            await obj.BuildAsync("Teste");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            await obj.CurrentBrowser!.CloseAsync();
            while (obj.CurrentBrowser is null || !obj.CurrentBrowser.IsConnected || obj.BufferLength < 2)
            {
                cts.Token.ThrowIfCancellationRequested();
                await Task.Delay(50, cts.Token);
            }
            var recoveredOnce = obj.CurrentBrowser;

            // When: the newly recovered browser also crashes. If the dead browser's handler
            // were still subscribed, this would fire two overlapping recoveries.
            await recoveredOnce.CloseAsync();

            // Then: the builder recovers again, cleanly, to a third distinct browser instance.
            while (obj.CurrentBrowser is null || !obj.CurrentBrowser.IsConnected || obj.BufferLength < 2)
            {
                cts.Token.ThrowIfCancellationRequested();
                await Task.Delay(50, cts.Token);
            }
            Assert.NotSame(recoveredOnce, obj.CurrentBrowser);
            Assert.Equal(2, obj.BufferLength);
            var page = obj.Acquire(cts.Token);
            Assert.NotNull(page);
        }
    }
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
#pragma warning restore IDE0079
}

