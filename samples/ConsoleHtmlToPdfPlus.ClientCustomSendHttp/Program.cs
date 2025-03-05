// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleHtmlToPdfPlus.ClientCustomSendHttp
{
    public class Program
    {
        private static IHost? HostApp = null;
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Example of HTML to PDF Plus (custom to save file at server) console");
            Console.WriteLine("  Using only Client with all settings sent via http to server PDF  ");
            Console.WriteLine("===================================================================");
            Console.WriteLine("");
            Console.WriteLine("Start the WebHtmlToPdf.CustomSaveFileServer Server project first. When ready, press any key to continue");
            Console.WriteLine("");
            Console.ReadKey();

            HostApp = CreateHostBuilder(args).Build();

            //token to gracefull shutdown
            var applifetime = HostApp.Services.GetService<IHostApplicationLifetime>()!;

            //client http to endpoint    
            var clienthttp = HostApp!.Services.GetRequiredService<IHttpClientFactory>().CreateClient("HtmlPdfServer");

             //create client instance  and send to server
            Console.WriteLine($"HtmlPdfClient send TemplateRazor to Server save PDF and replace custom-token at cloud like gcp/azure via http post");

            var lstprod = new List<Product>();
            for (int i = 0; i < 40; i++)
            {
                lstprod.Add(new Product($"Product{i}", 9.99m));
            }

            var order1 = new Order("Roberto Rivellino", "Rua S&atilde;o Jorge, 777", "+55 11 912345678", lstprod);

            // Generic suggestion for writing a file to a cloud like gcp/azure
            // Suggested return would be the full path "repo/filename"
            var paramTosave = new DataSavePDF("Filename.pdf","MyRepo","MyConnectionstring");

            var pdfresult = await HtmlPdfClient.Create("HtmlPdfPlusClient")
                                 .PageConfig((cfg) =>
                                 {
                                     cfg.Margins(10);
                                 })
                                 .Logger(HostApp.Services.GetService<ILogger<Program>>())
                                 .FromRazor(TemplateRazor(), order1)
                                 .Timeout(50000)
                                 .Run<DataSavePDF,string>(clienthttp,paramTosave, applifetime.ApplicationStopping);

            Console.WriteLine($"HtmlPdfClient IsSuccess {pdfresult.IsSuccess} after {pdfresult.ElapsedTime}");

            //Shwo result
            if (pdfresult.IsSuccess)
            {
                 Console.WriteLine($"File PDF generate at {pdfresult.OutputData}");
            }
            else
            {
                Console.WriteLine($"HtmlPdfClient error: {pdfresult.Error!}");
            }


            Console.WriteLine("Press any key to end");
            Console.ReadKey();


        }

        public record DataSavePDF(string Filename, string Folder, string ConnectionProvider);

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
                        httpClient.BaseAddress = new Uri("https://localhost:7239/SavePdf");
                    });
                });

        private static string TemplateRazor()
        {
            return """
                <!DOCTYPE html>
                <html lang="pt-br">
                <head>
                    <meta charset="UTF-8">
                    <title>[{FileName}] : Customer Details</title>
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
    }
}