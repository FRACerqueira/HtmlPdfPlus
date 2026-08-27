// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

namespace HtmlPdfPlus
{
    /// <summary>
    /// Readiness status of an <see cref="IHtmlPdfServer{Tin, Tout}"/> instance's underlying
    /// browser and page pool, as reported by <c>MapHtmlPdfHealthEndpoints</c>.
    /// </summary>
    /// <param name="BrowserConnected">Whether the underlying browser process is currently connected.</param>
    /// <param name="Recovering">Whether the browser is currently being restarted after an unexpected disconnect.</param>
    /// <param name="AvailablePages">The number of pages currently available in the pool.</param>
    /// <param name="PoolStarved">
    /// Whether the most recent recovery reconnected the browser but could not create a single
    /// usable page. <c>false</c> by default for callers/tests that predate this field.
    /// </param>
    /// <remarks>
    /// This is deliberately separate from per-request backpressure (<see cref="ErrorCode.PoolExhausted"/>):
    /// a momentarily saturated pool with <see cref="AvailablePages"/> at zero is still healthy
    /// and should keep receiving traffic, retried via the standard <c>Retry-After</c> signal.
    /// <see cref="PoolStarved"/> is a different condition: unlike a momentarily saturated pool,
    /// which self-corrects as soon as any in-flight request returns its page, a pool that came
    /// back from recovery with zero pages cannot self-correct - no request can ever acquire a
    /// page in the first place to later return one.
    /// </remarks>
    public sealed record HtmlPdfHealthStatus(bool BrowserConnected, bool Recovering, int AvailablePages, bool PoolStarved = false)
    {
        /// <summary>
        /// Gets a value indicating whether this instance can currently accept and process requests.
        /// </summary>
        public bool Healthy => BrowserConnected && !Recovering && !PoolStarved;
    }
}
