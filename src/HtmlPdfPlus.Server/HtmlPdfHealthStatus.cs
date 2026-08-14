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
    /// <remarks>
    /// This is deliberately separate from per-request backpressure (<see cref="ErrorCode.PoolExhausted"/>):
    /// a momentarily saturated pool with <see cref="AvailablePages"/> at zero is still healthy
    /// and should keep receiving traffic, retried via the standard <c>Retry-After</c> signal -
    /// only a disconnected or actively recovering browser makes this instance unable to make
    /// progress at all.
    /// </remarks>
    public sealed record HtmlPdfHealthStatus(bool BrowserConnected, bool Recovering, int AvailablePages)
    {
        /// <summary>
        /// Gets a value indicating whether this instance can currently accept and process requests.
        /// </summary>
        public bool Healthy => BrowserConnected && !Recovering;
    }
}
