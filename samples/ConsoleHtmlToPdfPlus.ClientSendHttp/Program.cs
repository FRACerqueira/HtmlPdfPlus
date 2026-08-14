// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleHtmlToPdfPlus.ClientSendHttp
{
    public class Program
    {
        private static readonly string PathToSamples = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private static IHost? HostApp = null;
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Example of HTML to PDF Plus console using only Client with all settings sent via http to server PDF");
            Console.WriteLine("===================================================================================================");
            Console.WriteLine(""); 
            Console.WriteLine("Start the WebHtmlToPdf.GenericServer Server project first. When ready, press any key to continue");
            Console.WriteLine("");
            Console.ReadKey();

            HostApp = CreateHostBuilder(args).Build();

             //token to gracefull shutdown
            var applifetime = HostApp.Services.GetService<IHostApplicationLifetime>()!;

            //client http to endpoint    
            var clienthttp = HostApp!.Services.GetRequiredService<IHttpClientFactory>().CreateClient("HtmlPdfServer");

            //create client instance and to HtmlPdfPlus server endpoint
            Console.WriteLine($"HtmlPdfClient send Html to PDF Server via http post");

            var pdfresult = await HtmlPdfClient.Create("HtmlPdfPlusClient")
                             .PageConfig((cfg) =>
                             {
                                 cfg.Margins(10)
                                   .Footer("'<span style=\"text-align: center;width: 100%;font-size: 10px\"> <span class=\"pageNumber\"></span> of <span class=\"totalPages\"></span></span>")
                                   .Header("'<span style=\"text-align: center;width: 100%;font-size: 10px\" class=\"title\"></span>")
                                   .Orientation(PageOrientation.Landscape)
                                   .DisplayHeaderFooter(true);
                             })
                             .Logger(HostApp.Services.GetService<ILogger<Program>>())
                             .FromHtml(HtmlSample())
                             .Timeout(5000)
                             .Run(clienthttp, applifetime.ApplicationStopping);

            Console.WriteLine($"HtmlPdfClient IsSuccess {pdfresult.IsSuccess} after {pdfresult.ElapsedTime}");

            //performs writing to file after performing conversion
            if (pdfresult.IsSuccess)
            {
                var fullpath = Path.Combine(PathToSamples, "html2pdfHtml.pdf");
                await File.WriteAllBytesAsync(fullpath, pdfresult.OutputData!);
                Console.WriteLine($"File PDF generate at {fullpath}");
            }
            else
            {
                Console.WriteLine($"HtmlPdfClient error: {pdfresult.Error!}");
            }

            Console.WriteLine("Press any key to next");
            Console.ReadKey();

            //create client instance  and send to server
            Console.WriteLine($"HtmlPdfClient send TemplateRazor to PDF Server via http post");

            var lstprod = new List<Product>();
            for (int i = 0; i < 40; i++)
            {
                lstprod.Add(new Product($"Product{i}", 9.99m));
            }

            var order1 = new Order("Roberto Rivellino", "Rua S&atilde;o Jorge, 777", "+55 11 912345678", lstprod);

            pdfresult = await HtmlPdfClient.Create("HtmlPdfPlusClient")
                                 .PageConfig((cfg) => cfg.Margins(10))
                                 .Logger(HostApp.Services.GetService<ILogger<Program>>())
                                 .FromRazor(TemplateRazor(), order1)
                                 .Timeout(5000)
                                 .Run(clienthttp,applifetime.ApplicationStopping);

            Console.WriteLine($"HtmlPdfClient IsSuccess {pdfresult.IsSuccess} after {pdfresult.ElapsedTime}");

            //performs writing to file after performing conversion
            if (pdfresult.IsSuccess)
            {
                var fullpath = Path.Combine(PathToSamples, "html2pdfRazorTemplate.pdf");
                await File.WriteAllBytesAsync(fullpath, pdfresult.OutputData!);
                Console.WriteLine($"File PDF generate at {fullpath}");
            }
            else
            {
                Console.WriteLine($"HtmlPdfClient error: {pdfresult.Error!}");
            }

            Console.WriteLine("Press any key to next");
            Console.ReadKey();

            //create client instance  and send to server
            Console.WriteLine($"HtmlPdfClient send Url to PDF Server via http post");

            pdfresult = await HtmlPdfClient.Create("HtmlPdfPlusClient")
                                .PageConfig((cfg) => cfg.Margins(10))
                                .Logger(HostApp.Services.GetService<ILogger<Program>>())
                                .FromUrl(new Uri("https://github.com/FRACerqueira/HtmlPdfPlus"))
                                .Timeout(15000)
                                .Run(clienthttp, applifetime.ApplicationStopping);

            Console.WriteLine($"HtmlPdfClient IsSuccess {pdfresult.IsSuccess} after {pdfresult.ElapsedTime}");

            //performs writing to file after performing conversion
            if (pdfresult.IsSuccess)
            {
                var fullpath = Path.Combine(PathToSamples, "HtmlPdfPlus.pdf");
                await File.WriteAllBytesAsync(fullpath, pdfresult.OutputData!);
                Console.WriteLine($"File PDF generate at {fullpath}");
            }
            else
            {
                Console.WriteLine($"HtmlPdfClient error: {pdfresult.Error!}");
            }


            Console.WriteLine("Press any key to end");
            Console.ReadKey();


        }

        public record Product(string Name, decimal Price);

        public record Order(string CustomerName, string CustomerAddress, string CustomerPhoneNumber, List<Product> Products);

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
                  services.AddHttpClient("HtmlPdfServer", httpClient =>
                  {
                      httpClient.BaseAddress = new Uri("https://localhost:7212/GeneratePdf");
                  });
                });

        private static string TemplateRazor()
        {
            return """
                <!DOCTYPE html>
                <html lang="pt-br">
                <head>
                    <meta charset="UTF-8">
                    <title>Customer Details</title>
                    <style>
                        table {
                            border-collapse: collapse;
                            width: 100%;
                        }
                        th, td {
                            border: 1px solid #ddd;
                            padding: 8px;
                        }
                        th {
                            background-color: #f4f4f4;
                            text-align: left;
                        }
                        tr { 
                            page-break-inside: avoid; 
                        }
                    </style>
                </head>
                <body>
                    <h1>Customer Details</h1>
                    <p><strong>Name:</strong> @Model.CustomerName</p>
                    <p><strong>Address:</strong> @Model.CustomerAddress</p>
                    <p><strong>Phone Number:</strong> @Model.CustomerPhoneNumber</p>

                    <h2>Products (@Model.Products.Count)</h2>
                    @if(Model.Products.Any())
                    {
                        <table>
                            <thead>
                                <tr>
                                    <th>Product Name</th>
                                    <th>Price</th>
                                </tr>
                            </thead>
                            <tbody>
                                @foreach (var product in Model.Products)
                                {
                                    <tr>
                                        <td>@product.Name</td>
                                        <td>@product.Price.ToString("C")</td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    } 
                    else
                    {
                        <p>No products found.</p>
                    }
                </body>
                </html>
                """;

        }

        private static string HtmlSample()
        {
            return
                """
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>HTML to PDF Test</title>
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
