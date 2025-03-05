// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Text;
using System.Text.Json;
using HtmlPdfPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperSimpleTcp;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).

namespace TcpServerHtmlToPdf.GenericServer
{
    public class Program
    {
        static readonly string _ListenerIp = "127.0.0.1";
        static readonly int _ListenerPort = 9000;
        static SimpleTcpServer? _ServerTCP;
        static bool _RunForever = true;
        static IHost? HostApp;
        public static void Main(string[] args)
        {
            HostApp = CreateHostBuilder(args).Build();

            Console.WriteLine("This example is not valid for production! It only demonstrates shipping flexibility");
            Console.WriteLine("===================================================================================");
            Console.WriteLine("");
            
            Console.WriteLine("Warmup HtmlPdfServerPlus with buffer");

            //Warmup HtmlPdfServerPlus on startup for better performance from the first request
            var WarmupTS = HostApp.WarmupHtmlPdfService();
            Console.WriteLine($"HtmlPdfServerPlus ready after {WarmupTS}");

            //token to gracefull shutdown
            var applifetime = HostApp.Services.GetService<IHostApplicationLifetime>()!;

            //set tcp server and start listener 
            _ServerTCP = new SimpleTcpServer(_ListenerIp, _ListenerPort);
            _ServerTCP.Settings.NoDelay = true;
            _ServerTCP.Events.ClientConnected += ClientConnected;
            _ServerTCP!.Events.ClientDisconnected += ClientDisconnected;
            _ServerTCP.Events.DataReceived += DataReceived;
            _ServerTCP.Events.DataSent += DataSent;

            _ServerTCP.Start();

            Console.WriteLine($"Start Server TCP {_ListenerIp}:{_ListenerPort} started press Q to end");

            while (_RunForever)
            {
                var key = Console.ReadKey();
                switch (key.KeyChar)
                {
                    case 'q':
                    case 'Q':
                        _RunForever = false;
                        break;
                }
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostContext, logbuilder) =>
                {
                    logbuilder
                        .SetMinimumLevel(LogLevel.Debug)
                        .AddFilter("Microsoft", LogLevel.Warning)
                        .AddFilter("System", LogLevel.Warning)
                        .AddConsole();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHtmlPdfService((cfg) =>
                    {
                        cfg.DefaultConfig((cfg) =>
                            {
                                cfg.DisplayHeaderFooter(true)
                                   .Margins(10);
                            })
                            .Logger(LogLevel.Debug, "MyPDFServer");
                    });
        });

        private static void DataSent(object sender, DataSentEventArgs e)
        {
            Console.WriteLine($"[{e.IpPort}] sent {e.BytesSent} bytes");
        }

        private static void ClientConnected(object sender, ConnectionEventArgs e)
        {
            Console.WriteLine($"[{ e.IpPort}] client connected {e.Reason}");
        }

        private static void ClientDisconnected(object sender, ConnectionEventArgs e)
        {
            Console.WriteLine($"[{e.IpPort}] client Disconnected {e.Reason}");
        }

        private static void DataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"[{e.IpPort}] Data Received {e.Data.Count} bytes");

            var PDFserver = HostApp!.Services.GetHtmlPdfService();

            var request = Encoding.UTF8.GetString(e.Data.Array!, 0, e.Data.Count);

            var aux = PDFserver.Run(request, CancellationToken.None).Result;

            var sendata = JsonSerializer.Serialize<HtmlPdfResult<byte[]>>(aux);

            _ServerTCP.Send(e.IpPort, sendata);
            _ServerTCP.Send(e.IpPort, [0]); //token end message
        }

     }
}
