// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus.Server.Core;

namespace TestHtmlPdfPlus.Behavioral
{
    /// <summary>
    /// Behavioral (given/when/then) regression for the close-during-use fix: a page must not
    /// be closed while work is still running on it, because Playwright's <c>PdfAsync</c> takes
    /// no cancellation token and cannot be aborted once started.
    /// </summary>
    public class ClosePageDuringUseBehaviorTests
    {
        [Fact]
        public async Task Given_PendingWork_When_CloseWhenSettled_Then_PageStaysOpenUntilWorkCompletes()
        {
            // Given: a real page, and work on it that has not finished yet.
            using var builder = new HtmlPdfBuilder(null);
            await builder.BuildAsync("test");
            var page = (await builder.AcquireAsync(CancellationToken.None))!;
            var pendingWork = new TaskCompletionSource();

            // When: CloseWhenSettled is asked to close the page once that work settles.
            builder.CloseWhenSettled(page, pendingWork.Task);

            // Then: while the work is still pending, the page must remain open and usable.
            await Task.Delay(100, CancellationToken.None);
            var whileStillPending = await Record.ExceptionAsync(() => page.EvaluateAsync<int>("1+1"));
            Assert.Null(whileStillPending);

            // When: the pending work finally completes.
            pendingWork.SetResult();
            await Task.Delay(200, CancellationToken.None);

            // Then: only now is the page actually closed.
            var afterSettled = await Record.ExceptionAsync(() => page.EvaluateAsync<int>("1+1"));
            Assert.NotNull(afterSettled);
        }

        [Fact]
        public async Task Given_PageWithPendingWork_When_ReplenishBufferAsync_Then_PoolCapacityIsRestoredImmediately()
        {
            // Given: a pool that just handed out its only page for still-pending work.
            using var builder = new HtmlPdfBuilder(null);
            builder.PagesBuffer(1);
            await builder.BuildAsync("test");
            var before = builder.BufferLength;
            await builder.AcquireAsync(CancellationToken.None);
            Assert.Equal(before - 1, builder.BufferLength);

            // When: the pool is replenished without waiting for that page to close.
            await builder.ReplenishBufferAsync();

            // Then: pool capacity is restored right away.
            Assert.Equal(before, builder.BufferLength);
        }
    }
}
