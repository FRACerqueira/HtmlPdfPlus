// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Diagnostics.Metrics;

namespace HtmlPdfPlus.Server.Core
{
    /// <summary>
    /// Standard <see cref="System.Diagnostics.Metrics"/> instruments for every
    /// <see cref="HtmlPdfBuilder"/> instance, so a host can observe pool depth, render duration
    /// and browser-restart count without the library dictating a specific metrics backend - a
    /// host wanting Prometheus/OTLP/Application Insights adds its own OpenTelemetry exporter and
    /// calls <c>AddMeter("HtmlPdfPlus")</c>.
    /// </summary>
    internal static class HtmlPdfMetrics
    {
        internal const string MeterName = "HtmlPdfPlus";

        internal static readonly Meter Meter = new(MeterName);

        /// <summary>
        /// Number of times the browser process was relaunched after an unexpected disconnect.
        /// Tagged with <c>sourcealias</c>.
        /// </summary>
        internal static readonly Counter<long> BrowserRestarts = Meter.CreateCounter<long>(
            "htmlpdfplus.browser.restarts",
            description: "Number of times the browser process was relaunched after an unexpected disconnect.");

        /// <summary>
        /// Duration of a single HTML-to-PDF request, from <c>Run</c> being called to the result
        /// being returned - spans the browser render itself plus any configured BeforePDF/AfterPDF
        /// hooks, since that is the same total the caller's own <c>HtmlPdfResult.ElapsedTime</c>
        /// reports. Tagged with <c>sourcealias</c> and <c>success</c>. Unlike <see cref="Errors"/>,
        /// this deliberately excludes requests that failed request-level validation (bad payload,
        /// decompression limit, out-of-range config) before a render was ever attempted -
        /// duration is meaningless for a request that never ran, so its count will not reconcile
        /// against <see cref="Errors"/> for a host taking a lot of malformed requests.
        /// </summary>
        internal static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
            "htmlpdfplus.request.duration",
            unit: "ms",
            description: "Duration of a single HTML-to-PDF request (render plus any BeforePDF/AfterPDF hooks), in milliseconds.");

        /// <summary>
        /// Number of failed requests, classified by their <see cref="ErrorCode"/> - including
        /// requests that failed request-level validation before a render was ever attempted, so
        /// this counter stays meaningful under a flood of malformed requests (see
        /// <see cref="RequestDuration"/> for the narrower, render-only counterpart). Tagged with
        /// <c>sourcealias</c> and <c>error_code</c>.
        /// </summary>
        internal static readonly Counter<long> Errors = Meter.CreateCounter<long>(
            "htmlpdfplus.errors",
            description: "Number of failed requests, tagged by ErrorCode.");

        /// <summary>
        /// Time spent waiting to acquire a page from the pool, regardless of whether the wait
        /// ended in a page being handed out, the pool's own AcquireTimeoutMs elapsing, or the
        /// caller's own deadline/cancellation firing first - all three are real time a request
        /// spent blocked on pool capacity. Tagged with <c>sourcealias</c> and <c>outcome</c>
        /// (<c>acquired</c>, <c>pool_exhausted</c>, or <c>canceled</c>). Distinct from
        /// <see cref="RequestDuration"/>, which also includes render/hook time, so pool
        /// contention can be told apart from a slow render.
        /// </summary>
        internal static readonly Histogram<double> AcquireWaitDuration = Meter.CreateHistogram<double>(
            "htmlpdfplus.pool.acquire_wait",
            unit: "ms",
            description: "Time spent waiting to acquire a page from the pool, in milliseconds.");
    }
}
