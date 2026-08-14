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
        /// reports. Tagged with <c>sourcealias</c> and <c>success</c>.
        /// </summary>
        internal static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
            "htmlpdfplus.request.duration",
            unit: "ms",
            description: "Duration of a single HTML-to-PDF request (render plus any BeforePDF/AfterPDF hooks), in milliseconds.");
    }
}
