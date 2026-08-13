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
            using var obj = new HtmlPdfBuilder();
            using var cts = new CancellationTokenSource();
            obj.PagesBuffer(1);
            await obj.BuildAsync("Teste");
            cts.CancelAfter(200);
            var firtpage = await obj.AcquireAsync(cts.Token);
            var page = await obj.AcquireAsync(cts.Token);
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
    }
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
#pragma warning restore IDE0079
}

