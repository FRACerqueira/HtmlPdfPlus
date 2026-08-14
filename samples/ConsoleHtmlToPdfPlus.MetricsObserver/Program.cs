// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Diagnostics.Metrics;
using System.Text;
using HtmlPdfPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleHtmlToPdfPlus.MetricsObserver
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Example: observing HtmlPdfPlus's built-in System.Diagnostics.Metrics instruments");
            Console.WriteLine("A real host does not need a MeterListener at all - it just calls AddMeter(\"HtmlPdfPlus\")");
            Console.WriteLine("once and plugs in whatever backend it already uses (Prometheus/OTLP/Application Insights).");
            Console.WriteLine("The MeterListener below exists only so this console sample can print measurements as they");
            Console.WriteLine("happen, with no extra package beyond the .NET runtime itself.");
            Console.WriteLine("====================================================================================================================");

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == "HtmlPdfPlus")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
                Console.WriteLine($"  [{instrument.Name}] +{measurement} {FormatTags(tags)}"));
            listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
                Console.WriteLine($"  [{instrument.Name}] {measurement:F1}{instrument.Unit} {FormatTags(tags)}"));
            listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
                Console.WriteLine($"  [{instrument.Name}] = {measurement} {FormatTags(tags)}"));
            listener.Start();

            var HostApp = CreateHostBuilder(args).Build();

            //token to gracefull shutdown
            var applifetime = HostApp.Services.GetService<IHostApplicationLifetime>()!;

            //instance of Html to Pdf Engine
            var PDFserver = HostApp!.Services.GetHtmlPdfService();

            Console.WriteLine();
            Console.WriteLine("-- htmlpdfplus.pool.available_pages before any load --");
            listener.RecordObservableInstruments();

            Console.WriteLine();
            Console.WriteLine("-- a successful conversion: htmlpdfplus.request.duration + htmlpdfplus.pool.acquire_wait[acquired] --");
            await PDFserver.ScopeData().FromHtml("<html><body><h1>Metrics demo</h1></body></html>").Run(applifetime.ApplicationStopping);

            Console.WriteLine();
            Console.WriteLine("-- a request-level validation failure: htmlpdfplus.errors only, NOT htmlpdfplus.request.duration --");
            Console.WriteLine("   (duration is meaningless for a request that never reached RunServer - see HtmlPdfMetrics.RequestDuration)");
            await PDFserver.ScopeData().FromHtml("<html><body>invalid timeout on purpose</body></html>", converttimeout: 0).Run(applifetime.ApplicationStopping);

            Console.WriteLine();
            Console.WriteLine("-- concurrent requests against a single-page pool: htmlpdfplus.pool.acquire_wait[pool_exhausted] --");
            var concurrent = Enumerable.Range(1, 8)
                .Select(i => PDFserver.ScopeData().FromHtml($"<html><body>Concurrent {i}</body></html>").Run(applifetime.ApplicationStopping));
            await Task.WhenAll(concurrent);

            Console.WriteLine();
            Console.WriteLine("-- htmlpdfplus.pool.available_pages after load --");
            listener.RecordObservableInstruments();

            Console.WriteLine();
            Console.WriteLine("Press any key");
            Console.ReadKey();
        }

        private static string FormatTags(ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var sb = new StringBuilder();
            foreach (var tag in tags)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(tag.Key).Append('=').Append(tag.Value);
            }
            return sb.Length == 0 ? string.Empty : $"({sb})";
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostContext, logbuilder) =>
                {
                    logbuilder
                        .SetMinimumLevel(LogLevel.Warning)
                        .AddFilter("Microsoft", LogLevel.Warning)
                        .AddFilter("System", LogLevel.Warning)
                        .AddConsole();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // A single page + short acquire timeout so the concurrent-requests step
                    // above reliably produces a pool_exhausted-tagged measurement too. 150ms is
                    // empirical (tuned to a ~90ms render on the machine this was written on) -
                    // raise it if every concurrent request fails on your machine, lower it if
                    // none do.
                    services.AddHtmlPdfService((cfg) =>
                    {
                        cfg.Logger(LogLevel.None, "MetricsDemo")
                           .PagesBuffer(1)
                           .AcquireTimeout(150);
                    }, sourceAlias: "MetricsDemo");
                });
    }
}
