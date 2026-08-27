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
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Trace)]
        public void Ensure_Logger_AcceptsAnyLogLevel_WithoutThrowing(LogLevel level)
        {
            var loggerfact = NullLoggerFactory.Instance;

            using var obj = new HtmlPdfBuilder(loggerfact);
            obj.Logger(level);
            Assert.Equal(level, obj.LevelLog);
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
            await obj.AcquireAsync(cts.Token);
            Assert.Equal(4, obj.BufferLength);
        }

        [Fact]
        public async Task Ensure_buid_With_AccquireTimeout()
        {
            using var obj = new HtmlPdfBuilder();
            obj.AcquireTimeout(20);
            using var cts = new CancellationTokenSource();
            obj.PagesBuffer(1);
            await obj.BuildAsync("Teste");
            cts.CancelAfter(200);
            var firtpage = await obj.AcquireAsync(cts.Token);
            var page = await obj.AcquireAsync(CancellationToken.None);
            Assert.NotNull(firtpage);
            Assert.Null(page);
        }

        [Fact]
        public async Task Ensure_buid_With_NotBufferExternalTimeout()
        {
            // The caller's own token (200ms) fires well before the pool's default
            // AcquireTimeoutMs (5000ms) - this is caller-driven, not a genuine pool
            // exhaustion, so AcquireAsync must propagate it instead of returning null (which
            // would misreport it as PoolExhausted upstream).
            using var obj = new HtmlPdfBuilder();
            using var cts = new CancellationTokenSource();
            obj.PagesBuffer(1);
            await obj.BuildAsync("Teste");
            cts.CancelAfter(200);
            var firtpage = await obj.AcquireAsync(cts.Token);
            Assert.NotNull(firtpage);
            await Assert.ThrowsAsync<OperationCanceledException>(() => obj.AcquireAsync(cts.Token));
        }


        [Fact]
        public async Task Ensure_buid_With_RestoreAvailableBuffer()
        {
            using var obj = new HtmlPdfBuilder();
            using var cts = new CancellationTokenSource();
            await obj.BuildAsync("Teste");
            cts.CancelAfter(100);
            var page = await obj.AcquireAsync(cts.Token);
            if (page is not null)
            {
                await obj.RestoreAvailableBuffer(page);
            }
            Assert.Equal(5, obj.BufferLength);
        }

        [Fact]
        public async Task Ensure_AcquireAsync_Completes_Once_Pool_Is_Replenished()
        {
            // Given: a pool that just handed out its only page, with nothing available.
            using var obj = new HtmlPdfBuilder();
            obj.PagesBuffer(1);
            await obj.BuildAsync("Teste");
            var firstPage = await obj.AcquireAsync(CancellationToken.None);
            Assert.NotNull(firstPage);

            // When: a caller starts waiting asynchronously for the next page before one exists,
            // proving the wait is a real async suspension and not a busy-poll loop that would
            // instead return null immediately.
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(5000);
            var pendingAcquire = obj.AcquireAsync(cts.Token);
            Assert.False(pendingAcquire.IsCompleted);

            // Then: it completes with the new page only after the pool is replenished.
            await obj.RestoreAvailableBuffer(firstPage!);
            var nextPage = await pendingAcquire;
            Assert.NotNull(nextPage);
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
            var page = await obj.AcquireAsync(cts.Token);
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
            var page = await obj.AcquireAsync(cts.Token);
            Assert.NotNull(page);
        }

        [Fact]
        public async Task Ensure_IsRecovering_IsFalse_BeforeCrashAndAfterRecoveryCompletes()
        {
            // Given: a built pool, not recovering.
            using var obj = new HtmlPdfBuilder();
            obj.PagesBuffer(2);
            await obj.BuildAsync("Teste");
            Assert.False(obj.IsRecovering);

            // When: the browser disconnects unexpectedly (same faithful CloseAsync simulation
            // used elsewhere in this suite) and auto-recovery runs to completion.
            await obj.CurrentBrowser!.CloseAsync();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (obj.CurrentBrowser is null || !obj.CurrentBrowser.IsConnected || obj.BufferLength < 2)
            {
                cts.Token.ThrowIfCancellationRequested();
                await Task.Delay(50, cts.Token);
            }

            // Then: a health check reading IsRecovering after recovery has fully completed must
            // not report a permanently degraded instance.
            Assert.False(obj.IsRecovering);
        }

        [Fact]
        public async Task Ensure_Pool_DoesNotOvershootPagesBuffer_When_InFlightPagesReturnAfterRecovery()
        {
            // Given: two pages checked out (buffer at 0) right before the browser dies.
            using var obj = new HtmlPdfBuilder();
            obj.PagesBuffer(2);
            await obj.BuildAsync("Teste");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var page1 = await obj.AcquireAsync(cts.Token);
            var page2 = await obj.AcquireAsync(cts.Token);

            // When: the browser disconnects unexpectedly and recovery refills the pool for the
            // NEW browser, then the two pages that were checked out from the OLD (now dead)
            // browser are returned - exactly what GeneratePDF's `finally` does for requests that
            // were mid-flight when the crash happened.
            await obj.CurrentBrowser!.CloseAsync();
            while (obj.CurrentBrowser is null || !obj.CurrentBrowser.IsConnected || obj.BufferLength < 2)
            {
                cts.Token.ThrowIfCancellationRequested();
                await Task.Delay(50, cts.Token);
            }
            Assert.Equal(2, obj.BufferLength);

            await obj.RestoreAvailableBuffer(page1!);
            await obj.RestoreAvailableBuffer(page2!);

            // Then: returning pages from the dead generation must not add on top of the capacity
            // recovery already restored for the current browser - the pool stays at PagesBuffer,
            // it does not silently double.
            Assert.Equal(2, obj.BufferLength);
        }

        [Theory]
        [InlineData("http://10.0.0.1", false)]
        [InlineData("http://172.16.5.4", false)]
        [InlineData("http://172.32.5.4", true)]
        [InlineData("http://192.168.1.1", false)]
        [InlineData("http://169.254.169.254", false)]
        [InlineData("http://127.0.0.1", false)]
        [InlineData("http://8.8.8.8", true)]
        [InlineData("http://example.com", true)]
        [InlineData("https://example.com", true)]
        [InlineData("ftp://example.com", false)]
        [InlineData("http://[::1]", false)]
        [InlineData("http://[fe80::1]", false)]
        [InlineData("http://[fc00::1]", false)]
        [InlineData("http://[2001:4860:4860::8888]", true)]
        public void Ensure_DefaultUrlPolicy_ClassifiesUrl(string url, bool expectedallowed)
        {
            Assert.Equal(expectedallowed, HtmlPdfBuilder.DefaultUrlPolicy(new Uri(url)));
        }

        [Fact]
        public void Ensure_UrlAllowPolicy_ThrowsArgumentNullException_WhenPolicyIsNull()
        {
            IHtmlPdfSrvBuilder obj = new HtmlPdfBuilder();
            Assert.Throws<ArgumentNullException>(() => obj.UrlAllowPolicy(null!));
            ((IDisposable)obj).Dispose();
        }

        [Fact]
        public void Ensure_UrlAllowPolicy_OverridesDefault()
        {
            using var obj = new HtmlPdfBuilder();
            var deniedByDefault = new Uri("http://169.254.169.254");
            Assert.False(obj.IsUrlAllowed(deniedByDefault));

            obj.UrlAllowPolicy(_ => true);

            Assert.True(obj.IsUrlAllowed(deniedByDefault));
        }

        [Fact]
        public void Ensure_MaxDecompressedRequestSize_DefaultValue()
        {
            using var obj = new HtmlPdfBuilder();
            Assert.Equal(52_428_800, obj.MaxDecompressedRequestSizeLimit);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Ensure_MaxDecompressedRequestSize_ThrowsArgumentException_WhenValueIsInvalid(long value)
        {
            IHtmlPdfSrvBuilder obj = new HtmlPdfBuilder();
            Assert.Throws<ArgumentException>(() => obj.MaxDecompressedRequestSize(value));
            ((IDisposable)obj).Dispose();
        }

        [Fact]
        public void Ensure_MaxDecompressedRequestSize_SetsValue()
        {
            using var obj = new HtmlPdfBuilder();
            obj.MaxDecompressedRequestSize(1024);
            Assert.Equal(1024, obj.MaxDecompressedRequestSizeLimit);
        }
    }
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
#pragma warning restore IDE0079
}

