// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using Microsoft.Extensions.Logging;
using Moq;
using HtmlPdfPlus.Client.Core;
using HtmlPdfPlus;

namespace TestHtmlPdfPlus.HtmlPdfCliPlus
{
    public class HtmlPdfClientInstanceTests
    {
        private readonly HttpClient _mockHttpClient;
        private readonly Mock<ILogger> _mockLogger;
        private readonly DisableOptionsHtmlToPdf _disableOptions;
        private readonly HtmlPdfClientInstance _clientInstance;

        public HtmlPdfClientInstanceTests()
        {
            _mockHttpClient = new HttpClient()
            {
                BaseAddress = new Uri("http://localhost:8099"),
                Timeout = TimeSpan.FromMilliseconds(1),
            };
            _mockLogger = new Mock<ILogger>();
            _disableOptions = DisableOptionsHtmlToPdf.EnabledAllFeatures;
            _clientInstance = new HtmlPdfClientInstance("testAlias", _disableOptions);
        }

        [Fact]
        public void PageConfig_ShouldSetPdfPageConfig()
        {
            _clientInstance.PageConfig(config => config.Margins(10));
            Assert.NotNull(_clientInstance);
        }

        [Fact]
        public void FromHtml_ShouldSetHtml()
        {
            _clientInstance.FromHtml("<html></html>");
            Assert.NotNull(_clientInstance);
        }

        [Fact]
        public void FromUrl_ShouldSetHtml()
        {
            var uri = new Uri("http://example.com");
            _clientInstance.FromUrl(uri);
            Assert.NotNull(_clientInstance);
        }

        [Fact]
        public void FromRazor_ShouldSetHtml()
        {
            var lstprod = new List<Product>
            {
                new("Product", 9.99m)
            };
            var order1 = new Order("Roberto Rivellino", "Rua S&atilde;o Jorge, 777", "+55 11 912345678", lstprod);
            _clientInstance.FromRazor(TemplateRazor(), order1);
            Assert.NotNull(_clientInstance);
        }

        [Fact]
        public void Logger_ShouldThrowArgumentException_WhenLogLevelIsInvalid()
        {
            Assert.Throws<ArgumentException>(() => _clientInstance.Logger(_mockLogger.Object, LogLevel.Error));
        }

        [Fact]
        public void Logger_ShouldSetLogger()
        {
            _clientInstance.Logger(_mockLogger.Object, LogLevel.Debug);
            Assert.NotNull(_clientInstance);
        }

        [Fact]
        public void Timeout_ShouldThrowArgumentException_WhenValueIsInvalid()
        {
            Assert.Throws<ArgumentException>(() => _clientInstance.Timeout(0));
        }

        [Fact]
        public void Timeout_ShouldSetTimeout()
        {
            _clientInstance.Timeout(10000);
            Assert.NotNull(_clientInstance);
        }

        [Fact]
        public void HtmlParser_ShouldSetHtmlParser()
        {
            _clientInstance.HtmlParser(true, error => { });
            Assert.NotNull(_clientInstance);
        }

        [Fact]
        public async Task Run_ShouldThrowInvalidOperationException_WhenHtmlIsEmpty()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => _clientInstance.Run((html, token) => Task.FromResult(new HtmlPdfResult<byte[]>(true, false, TimeSpan.Zero, [])), CancellationToken.None));
        }

        [Fact]
        public async Task Run_ShouldReturnHtmlPdfResult()
        {
            _clientInstance.FromHtml("<html><body><h1>test</h1></body></html>");
            var result = await _clientInstance.Run((html, token) => Task.FromResult(new HtmlPdfResult<byte[]>(true, false, TimeSpan.Zero, [])), CancellationToken.None);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Run_HttpClient_ShouldThrowInvalidOperationException_WhenHtmlIsEmpty()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => _clientInstance.Run(new HttpClient(), CancellationToken.None));
        }

        [Fact]
        public async Task Run_HttpClient_ShouldReturnHtmlPdfResult()
        {
            _clientInstance.FromHtml("<html><body><h1>test</h1></body></html>");
            var result = await _clientInstance.Run(_mockHttpClient, CancellationToken.None);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Run_HttpClient_WithEndpoint_ShouldThrowInvalidOperationException_WhenHtmlIsEmpty()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => _clientInstance.Run(new HttpClient(), "http://example.com", CancellationToken.None));
        }

        [Fact]
        public async Task Run_HttpClient_WithEndpoint_ShouldReturnHtmlPdfResult()
        {
            _clientInstance.FromHtml("<html><body><h1>test</h1></body></html>");
            var result = await _clientInstance.Run(_mockHttpClient, "http://example.com", CancellationToken.None);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Run_CustomData_ShouldThrowInvalidOperationException_WhenHtmlIsEmpty()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => _clientInstance.Run<object, byte[]>(new HttpClient(), null, CancellationToken.None));
        }

        [Fact]
        public async Task Run_CustomData_ShouldReturnHtmlPdfResult()
        {
            _clientInstance.FromHtml("<html><body><h1>test</h1></body></html>");
            var result = await _clientInstance.Run<object, byte[]>(_mockHttpClient, null, CancellationToken.None);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Run_CustomData_WithEndpoint_ShouldThrowInvalidOperationException_WhenHtmlIsEmpty()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => _clientInstance.Run<object, byte[]>(new HttpClient(), "http://example.com", null, CancellationToken.None));
        }

        [Fact]
        public async Task Run_CustomData_WithEndpoint_ShouldReturnHtmlPdfResult()
        {
            _clientInstance.FromHtml("<html><body><h1>test</h1></body></html>");
            var result = await _clientInstance.Run<object, byte[]>(_mockHttpClient, "http://example.com", null, CancellationToken.None);
            Assert.NotNull(result);
        }

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

        public record Product(string Name, decimal Price);

        public record Order(string CustomerName, string CustomerAddress, string CustomerPhoneNumber, List<Product> Products);
    }
}