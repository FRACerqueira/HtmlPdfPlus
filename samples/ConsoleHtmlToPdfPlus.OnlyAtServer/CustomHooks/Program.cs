// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleHtmlToPdfPlus.OnlyAtServerCustomHooks
{
    public class Program
    {
        private static readonly string PathToSamples = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Example: server-only conversion with BeforePDF/AfterPDF hooks (token substitution, custom file output)");
            Console.WriteLine("=====================================================================================================================================================");

            var HostApp = CreateHostBuilder(args).Build();

            Console.WriteLine("Warmup HtmlPdfServerPlus with buffer");

            var WarmupTS = HostApp.WarmupHtmlPdfService<string, string>();
            Console.WriteLine($"HtmlPdfServerPlus ready after {WarmupTS}");

            var applifetime = HostApp.Services.GetService<IHostApplicationLifetime>()!;

            var PDFserver = HostApp!.Services.GetHtmlPdfService<string, string>();

            var pdfresult = await PDFserver
                .ScopeData(Path.Combine(PathToSamples, "html2pdfHtml.pdf"))
                .FromHtml(HtmlSample(), 5000)
                .BeforePDF((html, _, _) =>
                {
                    var aux = html.Replace("[{MyTokenTemplate}]", "HTML to PDF Test");
                    return Task.FromResult(aux);
                })
                .AfterPDF(async (pdfbyte, filepath, token) =>
                {
                    await File.WriteAllBytesAsync(filepath!, pdfbyte!, token);
                    Console.WriteLine($"File PDF generate at {filepath}");
                    return filepath!;
                })
                .Run(applifetime.ApplicationStopping);

            Console.WriteLine($"HtmlPdfServer IsSuccess {pdfresult.IsSuccess} after {pdfresult.ElapsedTime}");

            if (pdfresult.IsSuccess)
            {
                Console.WriteLine($"File PDF generate at {pdfresult.OutputData}");
            }
            else
            {
                Console.WriteLine($"HtmlPdfServer error: {pdfresult.Error}");
            }
            Console.WriteLine("Press any key");
            Console.ReadKey();

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
                    services.AddHtmlPdfService<string, string>((cfg) =>
                    {
                        cfg.DefaultConfig((cfg) =>
                            {
                                cfg.DisplayHeaderFooter(true)
                                   .Margins(10, 10, 10, 10);
                            })
                            .Logger(LogLevel.Debug, "MyPDFServer");
                        // Note: DisableOptionsHtmlToPdf.DisableCompress only affects the
                        // ScopeRequest(bytes)/Run(bytes) path, which decompresses an incoming
                        // payload - ScopeData() below never receives one, so there is nothing to
                        // compress or decompress in this same-process scenario regardless of this
                        // flag. See docs/guide/architecture.md for the ScopeData vs ScopeRequest
                        // distinction.
                    });
                });

        // The [{MyTokenTemplate}] placeholder below is substituted by the BeforePDF hook in
        // Main before conversion - that substitution is the whole point of this sample.
        private static string HtmlSample()
        {
            return
                """
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>[{MyTokenTemplate}]</title>
                    <style>
                        body {
                            font-family: Arial, sans-serif;
                            margin: 0;
                            padding: 0;
                            background-color: #f0f8ff;
                        }
                        header {
                            background-color: #ff7f50;
                            color: white;
                            text-align: center;
                            padding: 20px;
                        }
                        nav {
                            display: flex;
                            justify-content: space-around;
                            background-color: #4682b4;
                            padding: 10px;
                        }
                        nav a {
                            color: white;
                            text-decoration: none;
                        }
                        section {
                            padding: 20px;
                            display: flex;
                            flex-wrap: wrap;
                            gap: 20px;
                            background-color: #f5f5f5;
                        }
                        article {
                            background-color: #ffefd5;
                            border: 2px solid #deb887;
                            padding: 15px;
                            flex: 1 1 calc(33.33% - 40px);
                            box-shadow: 2px 2px 5px rgba(0,0,0,0.3);
                        }
                        footer {
                            text-align: center;
                            background-color: #2e8b57;
                            color: white;
                            padding: 10px;
                        }
                        .base64-image {
                            width: 100%;
                            max-width: 300px;
                            display: block;
                            margin: 0 auto;
                        }
                        .color-box {
                            width: 100px;
                            height: 100px;
                            display: inline-block;
                            margin: 5px;
                        }
                        form {
                            background-color: #e6e6fa;
                            padding: 20px;
                            margin: 20px 0;
                            border: 2px solid #8a2be2;
                            border-radius: 10px;
                        }
                        form label {
                            display: block;
                            margin: 10px 0 5px;
                        }
                        form input, form textarea, form select, form button {
                            width: 100%;
                            padding: 10px;
                            margin-bottom: 10px;
                            border: 1px solid #ccc;
                            border-radius: 5px;
                        }
                    </style>
                </head>
                <body>
                    <header>
                        <h1>Test HTML to PDF Conversion</h1>
                        <p>A page with diverse elements to test your tool</p>
                    </header>
                    <nav>
                        <a href="https://www.microsoft.com">Microsoft</a>
                        <a href="https://www.google.com">Google</a>
                        <a href="https://github.com/FRACerqueira/HtmlPdfPlus">HtmlPdfPlus</a>
                    </nav>
                    <section>
                        <article>
                            <h2>Article 1</h2>
                            <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque facilisis.</p>
                            <img src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/w8AAn8C4iO5fwAAAABJRU5ErkJggg==" alt="Black dot" class="base64-image">
                        </article>
                        <article>
                            <h2>Article 2</h2>
                            <p>Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.</p>
                            <img src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg==" alt="Blue square" class="base64-image">
                        </article>
                        <article>
                            <h2>Article 3</h2>
                            <p>Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.</p>
                            <img src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/w8AAn8C4iO5fwAAAABJRU5ErkJggg==" alt="Black dot" class="base64-image">
                        </article>
                    </section>
                    <section>
                        <h2>Colors</h2>
                        <div class="color-box" style="background-color: red;"></div>
                        <div class="color-box" style="background-color: green;"></div>
                        <div class="color-box" style="background-color: blue;"></div>
                        <div class="color-box" style="background-color: yellow;"></div>
                        <div class="color-box" style="background-color: purple;"></div>
                    </section>
                    <section>
                        <h2>Feedback Form</h2>
                        <form action="#" method="post">
                            <label for="name">Name:</label>
                            <input type="text" id="name" name="name" placeholder="Enter your name">

                            <label for="email">Email:</label>
                            <input type="text" id="email" name="email" placeholder="Enter your email">

                            <label for="message">Message:</label>
                            <textarea id="message" name="message" rows="5" placeholder="Your message..."></textarea>

                            <label for="rating">Rating:</label>
                            <select id="rating" name="rating">
                                <option value="excellent">Excellent</option>
                                <option value="good">Good</option>
                                <option value="average">Average</option>
                                <option value="poor">Poor</option>
                            </select>

                            <button type="submit">Submit</button>
                        </form>
                    </section>
        
                    <footer>
                        <p>&copy; 2024 HTML to PDF Test Page</p>
                    </footer>
                </body>
                </html>
                """;
        }
    }
}