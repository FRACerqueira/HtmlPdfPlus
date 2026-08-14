// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleHtmlToPdfPlus.RetryAfterBackpressure
{
    public class Program
    {
        // Deliberately tiny: a single page and a short acquire timeout so a handful of
        // concurrent requests reliably contends for the one page and at least one of them
        // observes ErrorCode.PoolExhausted, instead of relying on a slow real-world render.
        private const int Concurrency = 10;
        private const int MaxAttemptsPerRequest = 8;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Example: detecting ErrorCode.PoolExhausted and backing off using ErrorInfo.RetryAfterSeconds");
            Console.WriteLine("=================================================================================================");

            var HostApp = CreateHostBuilder(args).Build();

            //token to gracefull shutdown
            var applifetime = HostApp.Services.GetService<IHostApplicationLifetime>()!;

            //instance of Html to Pdf Engine
            var PDFserver = HostApp!.Services.GetHtmlPdfService();

            Console.WriteLine($"Firing {Concurrency} concurrent requests against a pool of 1 page - expect some to hit PoolExhausted and retry");
            Console.WriteLine("");

            var requests = Enumerable.Range(1, Concurrency)
                .Select(requestId => RunWithRetry(PDFserver, requestId, applifetime.ApplicationStopping));
            await Task.WhenAll(requests);

            Console.WriteLine("");
            Console.WriteLine("Press any key");
            Console.ReadKey();
        }

        // The caller's own retry loop: a real client (over HTTP, TCP, or in-process like here)
        // owns this decision - the library only classifies the failure and suggests a delay via
        // ErrorInfo.RetryAfterSeconds, it never retries on the caller's behalf.
        private static async Task RunWithRetry(IHtmlPdfServer<object, byte[]> server, int requestId, CancellationToken token)
        {
            for (var attempt = 1; attempt <= MaxAttemptsPerRequest; attempt++)
            {
                var result = await server
                    .ScopeData()
                    .FromHtml($"<html><body><h1>Request {requestId}</h1><p>Attempt {attempt}</p></body></html>")
                    .Run(token);

                if (result.IsSuccess)
                {
                    Console.WriteLine($"Request {requestId}: succeeded on attempt {attempt} after {result.ElapsedTime}");
                    return;
                }

                if (result.Error?.Code == ErrorCode.PoolExhausted && result.Error.RetryAfterSeconds is int retryAfterSeconds)
                {
                    Console.WriteLine($"Request {requestId}: pool exhausted on attempt {attempt}, retrying in {retryAfterSeconds}s");
                    await Task.Delay(TimeSpan.FromSeconds(retryAfterSeconds), token);
                    continue;
                }

                // Any other failure (validation, timeout, browser error) is not backpressure -
                // retrying blindly would not help, so it is reported and not retried here.
                Console.WriteLine($"Request {requestId}: failed with {result.Error}");
                return;
            }

            Console.WriteLine($"Request {requestId}: gave up after {MaxAttemptsPerRequest} attempts");
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
                    services.AddHtmlPdfService((cfg) =>
                    {
                        // 150ms is empirical, not principled: tuned so a ~90ms render on the
                        // machine this was written on reliably overruns it for later requests
                        // in the batch. If every request fails on your machine, raise this; if
                        // none do, lower it (or increase Concurrency above).
                        cfg.Logger(LogLevel.None, "RetryAfterDemo")
                           .PagesBuffer(1)
                           .AcquireTimeout(150);
                    });
                });
    }
}
