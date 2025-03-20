﻿// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleHtmlToPdfPlus.OnlyAtServerV1
{
    public class Program
    {
        private static readonly string PathToSamples = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Example of HTML console for PDF Plus using only server with all settings also on the server");
            Console.WriteLine("===========================================================================================");

            var HostApp = CreateHostBuilder(args).Build();

            Console.WriteLine("Warmup HtmlPdfServerPlus with buffer");

            //Warmup HtmlPdfServerPlus on startup for better performance from the first request
            var WarmupTS = HostApp.WarmupHtmlPdfService<string, string>();
            Console.WriteLine($"HtmlPdfServerPlus ready after {WarmupTS}");

            //token to gracefull shutdown
            var applifetime = HostApp.Services.GetService<IHostApplicationLifetime>()!;

            //instance of Html to Pdf Engine
            var PDFserver = HostApp!.Services.GetHtmlPdfService<string, string>();

            //Performs conversion and custom operations on the server
            var pdfresult = await PDFserver
                .ScopeData(Path.Combine(PathToSamples, "html2pdfHtml.pdf"))
                .FromHtml(HtmlSample(), 5000)
                .BeforePDF((html, _, _) =>
                {
                    //performs replacement token substitution in the HTML source before performing the conversion
                    var aux = html.Replace("[{MyTokenTemplate}]", "HTML to PDF Test");
                    return Task.FromResult(aux);
                })
                .AfterPDF(async (pdfbyte, filepath, token) =>
                {
                    //performs writing to file after performing conversion
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
                    //create an Html2Pdf ServerPlus service with input/output parameter being a file path
                    services.AddHtmlPdfService<string, string>((cfg) =>
                    {
                        cfg.DefaultConfig((cfg) =>
                            {
                                cfg.DisplayHeaderFooter(true)
                                   .Margins(10, 10, 10, 10);
                            })
                            .Logger(LogLevel.Debug, "MyPDFServer")
                            //when run in the same context, not Compress is fast because it is not required to transfer data over the network
                            .DisableFeatures(DisableOptionsHtmlToPdf.DisableCompress);
                    });
                });

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
                        <a href="file:./myfile.txt</a>
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
                        <article>
                        <h2>Article 4</h2>
                            <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque facilisis.</p>
                            <img src="https://via.placeholder.com/300" alt="Placeholder Image" class="base64-image">
                        </article>
                            <article>
                                <h2>Article 5</h2>
                                <p>Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.</p>
                                <img src="https://via.placeholder.com/150" alt="Placeholder Image" class="base64-image">
                            </article>
                            <article>
                                <h2>Article 6</h2>
                                <p>Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.</p>
                                <img src="https://via.placeholder.com/100" alt="Placeholder Image" class="base64-image">
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
        
                <br />
    
                    <pre>
                                         40m     41m     42m     43m     44m     45m     46m     47m
                                <span style="">  gYw  </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; background-color: dimgray; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; background-color: red; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; background-color: lime; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; background-color: yellow; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; background-color: #3333FF; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; background-color: fuchsia; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; background-color: aqua; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; background-color: white; ">  gYw  </span>
                        <span style="">    1m </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; ">  gYw   </span><span style="font-weight: bold; "></span><span style="font-weight: bold; background-color: dimgray; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; "></span><span style="font-weight: bold; background-color: red; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; "></span><span style="font-weight: bold; background-color: lime; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; "></span><span style="font-weight: bold; background-color: yellow; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; "></span><span style="font-weight: bold; background-color: #3333FF; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; "></span><span style="font-weight: bold; background-color: fuchsia; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; "></span><span style="font-weight: bold; background-color: aqua; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; "></span><span style="font-weight: bold; background-color: white; ">  gYw  </span>
                        <span style="">   30m </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; ">  gYw   </span><span style="color: dimgray; "></span><span style="background-color: dimgray; color: dimgray; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; "></span><span style="background-color: red; color: dimgray; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; "></span><span style="background-color: lime; color: dimgray; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; "></span><span style="background-color: yellow; color: dimgray; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; "></span><span style="background-color: #3333FF; color: dimgray; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; "></span><span style="background-color: fuchsia; color: dimgray; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; "></span><span style="background-color: aqua; color: dimgray; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; "></span><span style="background-color: white; color: dimgray; ">  gYw  </span>
                        <span style=""> 1;30m </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; ">  gYw   </span><span style="font-weight: bold; color: dimgray; "></span><span style="font-weight: bold; background-color: dimgray; color: dimgray; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; "></span><span style="font-weight: bold; background-color: red; color: dimgray; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; "></span><span style="font-weight: bold; background-color: lime; color: dimgray; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; "></span><span style="font-weight: bold; background-color: yellow; color: dimgray; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; "></span><span style="font-weight: bold; background-color: #3333FF; color: dimgray; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; "></span><span style="font-weight: bold; background-color: fuchsia; color: dimgray; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; "></span><span style="font-weight: bold; background-color: aqua; color: dimgray; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: dimgray; "></span><span style="font-weight: bold; background-color: white; color: dimgray; ">  gYw  </span>
                        <span style="">   31m </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; ">  gYw   </span><span style="color: red; "></span><span style="background-color: dimgray; color: red; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; "></span><span style="background-color: red; color: red; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; "></span><span style="background-color: lime; color: red; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; "></span><span style="background-color: yellow; color: red; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; "></span><span style="background-color: #3333FF; color: red; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; "></span><span style="background-color: fuchsia; color: red; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; "></span><span style="background-color: aqua; color: red; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; "></span><span style="background-color: white; color: red; ">  gYw  </span>
                        <span style=""> 1;31m </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; ">  gYw   </span><span style="font-weight: bold; color: red; "></span><span style="font-weight: bold; background-color: dimgray; color: red; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; "></span><span style="font-weight: bold; background-color: red; color: red; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; "></span><span style="font-weight: bold; background-color: lime; color: red; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; "></span><span style="font-weight: bold; background-color: yellow; color: red; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; "></span><span style="font-weight: bold; background-color: #3333FF; color: red; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; "></span><span style="font-weight: bold; background-color: fuchsia; color: red; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; "></span><span style="font-weight: bold; background-color: aqua; color: red; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: red; "></span><span style="font-weight: bold; background-color: white; color: red; ">  gYw  </span>
                        <span style="">   32m </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; ">  gYw   </span><span style="color: lime; "></span><span style="background-color: dimgray; color: lime; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; "></span><span style="background-color: red; color: lime; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; "></span><span style="background-color: lime; color: lime; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; "></span><span style="background-color: yellow; color: lime; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; "></span><span style="background-color: #3333FF; color: lime; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; "></span><span style="background-color: fuchsia; color: lime; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; "></span><span style="background-color: aqua; color: lime; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; "></span><span style="background-color: white; color: lime; ">  gYw  </span>
                        <span style=""> 1;32m </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; ">  gYw   </span><span style="font-weight: bold; color: lime; "></span><span style="font-weight: bold; background-color: dimgray; color: lime; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; "></span><span style="font-weight: bold; background-color: red; color: lime; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; "></span><span style="font-weight: bold; background-color: lime; color: lime; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; "></span><span style="font-weight: bold; background-color: yellow; color: lime; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; "></span><span style="font-weight: bold; background-color: #3333FF; color: lime; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; "></span><span style="font-weight: bold; background-color: fuchsia; color: lime; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; "></span><span style="font-weight: bold; background-color: aqua; color: lime; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: lime; "></span><span style="font-weight: bold; background-color: white; color: lime; ">  gYw  </span>
                        <span style="">   33m </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; ">  gYw   </span><span style="color: yellow; "></span><span style="background-color: dimgray; color: yellow; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; "></span><span style="background-color: red; color: yellow; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; "></span><span style="background-color: lime; color: yellow; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; "></span><span style="background-color: yellow; color: yellow; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; "></span><span style="background-color: #3333FF; color: yellow; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; "></span><span style="background-color: fuchsia; color: yellow; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; "></span><span style="background-color: aqua; color: yellow; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; "></span><span style="background-color: white; color: yellow; ">  gYw  </span>
                        <span style=""> 1;33m </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; ">  gYw   </span><span style="font-weight: bold; color: yellow; "></span><span style="font-weight: bold; background-color: dimgray; color: yellow; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; "></span><span style="font-weight: bold; background-color: red; color: yellow; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; "></span><span style="font-weight: bold; background-color: lime; color: yellow; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; "></span><span style="font-weight: bold; background-color: yellow; color: yellow; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; "></span><span style="font-weight: bold; background-color: #3333FF; color: yellow; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; "></span><span style="font-weight: bold; background-color: fuchsia; color: yellow; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; "></span><span style="font-weight: bold; background-color: aqua; color: yellow; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: yellow; "></span><span style="font-weight: bold; background-color: white; color: yellow; ">  gYw  </span>
                        <span style="">   34m </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; ">  gYw   </span><span style="color: #3333FF; "></span><span style="background-color: dimgray; color: #3333FF; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; "></span><span style="background-color: red; color: #3333FF; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; "></span><span style="background-color: lime; color: #3333FF; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; "></span><span style="background-color: yellow; color: #3333FF; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; "></span><span style="background-color: #3333FF; color: #3333FF; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; "></span><span style="background-color: fuchsia; color: #3333FF; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; "></span><span style="background-color: aqua; color: #3333FF; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; "></span><span style="background-color: white; color: #3333FF; ">  gYw  </span>
                        <span style=""> 1;34m </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; ">  gYw   </span><span style="font-weight: bold; color: #3333FF; "></span><span style="font-weight: bold; background-color: dimgray; color: #3333FF; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; "></span><span style="font-weight: bold; background-color: red; color: #3333FF; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; "></span><span style="font-weight: bold; background-color: lime; color: #3333FF; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; "></span><span style="font-weight: bold; background-color: yellow; color: #3333FF; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; "></span><span style="font-weight: bold; background-color: #3333FF; color: #3333FF; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; "></span><span style="font-weight: bold; background-color: fuchsia; color: #3333FF; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; "></span><span style="font-weight: bold; background-color: aqua; color: #3333FF; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: #3333FF; "></span><span style="font-weight: bold; background-color: white; color: #3333FF; ">  gYw  </span>
                        <span style="">   35m </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; ">  gYw   </span><span style="color: fuchsia; "></span><span style="background-color: dimgray; color: fuchsia; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; "></span><span style="background-color: red; color: fuchsia; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; "></span><span style="background-color: lime; color: fuchsia; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; "></span><span style="background-color: yellow; color: fuchsia; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; "></span><span style="background-color: #3333FF; color: fuchsia; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; "></span><span style="background-color: fuchsia; color: fuchsia; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; "></span><span style="background-color: aqua; color: fuchsia; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; "></span><span style="background-color: white; color: fuchsia; ">  gYw  </span>
                        <span style=""> 1;35m </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; ">  gYw   </span><span style="font-weight: bold; color: fuchsia; "></span><span style="font-weight: bold; background-color: dimgray; color: fuchsia; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; "></span><span style="font-weight: bold; background-color: red; color: fuchsia; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; "></span><span style="font-weight: bold; background-color: lime; color: fuchsia; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; "></span><span style="font-weight: bold; background-color: yellow; color: fuchsia; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; "></span><span style="font-weight: bold; background-color: #3333FF; color: fuchsia; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; "></span><span style="font-weight: bold; background-color: fuchsia; color: fuchsia; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; "></span><span style="font-weight: bold; background-color: aqua; color: fuchsia; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: fuchsia; "></span><span style="font-weight: bold; background-color: white; color: fuchsia; ">  gYw  </span>
                        <span style="">   36m </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; ">  gYw   </span><span style="color: aqua; "></span><span style="background-color: dimgray; color: aqua; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; "></span><span style="background-color: red; color: aqua; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; "></span><span style="background-color: lime; color: aqua; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; "></span><span style="background-color: yellow; color: aqua; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; "></span><span style="background-color: #3333FF; color: aqua; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; "></span><span style="background-color: fuchsia; color: aqua; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; "></span><span style="background-color: aqua; color: aqua; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; "></span><span style="background-color: white; color: aqua; ">  gYw  </span>
                        <span style=""> 1;36m </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; ">  gYw   </span><span style="font-weight: bold; color: aqua; "></span><span style="font-weight: bold; background-color: dimgray; color: aqua; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; "></span><span style="font-weight: bold; background-color: red; color: aqua; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; "></span><span style="font-weight: bold; background-color: lime; color: aqua; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; "></span><span style="font-weight: bold; background-color: yellow; color: aqua; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; "></span><span style="font-weight: bold; background-color: #3333FF; color: aqua; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; "></span><span style="font-weight: bold; background-color: fuchsia; color: aqua; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; "></span><span style="font-weight: bold; background-color: aqua; color: aqua; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: aqua; "></span><span style="font-weight: bold; background-color: white; color: aqua; ">  gYw  </span>
                        <span style="">   37m </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; ">  gYw   </span><span style="color: white; "></span><span style="background-color: dimgray; color: white; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; "></span><span style="background-color: red; color: white; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; "></span><span style="background-color: lime; color: white; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; "></span><span style="background-color: yellow; color: white; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; "></span><span style="background-color: #3333FF; color: white; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; "></span><span style="background-color: fuchsia; color: white; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; "></span><span style="background-color: aqua; color: white; ">  gYw  </span><span style=""> </span><span style="background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; "></span><span style="background-color: white; color: white; ">  gYw  </span>
                        <span style=""> 1;37m </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; ">  gYw   </span><span style="font-weight: bold; color: white; "></span><span style="font-weight: bold; background-color: dimgray; color: white; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; "></span><span style="font-weight: bold; background-color: red; color: white; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; "></span><span style="font-weight: bold; background-color: lime; color: white; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; "></span><span style="font-weight: bold; background-color: yellow; color: white; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; "></span><span style="font-weight: bold; background-color: #3333FF; color: white; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; "></span><span style="font-weight: bold; background-color: fuchsia; color: white; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; "></span><span style="font-weight: bold; background-color: aqua; color: white; ">  gYw  </span><span style=""> </span><span style="font-weight: bold; background-color: initial; color: initial; font-weight: normal; opacity: 1.0; font-style: normal; text-decoration: none; display: inline; color: white; "></span><span style="font-weight: bold; background-color: white; color: white; ">  gYw  </span>
                    </pre>
        
                    <footer>
                        <p>&copy; 2024 HTML to PDF Test Page</p>
                    </footer>
                </body>
                </html>
                """;
        }
    }
}