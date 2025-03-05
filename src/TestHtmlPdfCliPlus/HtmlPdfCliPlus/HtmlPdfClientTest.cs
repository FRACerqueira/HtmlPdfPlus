// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using HtmlPdfShrPlus.Core;

namespace TestHtmlPdfPlus.HtmlPdfCliPlus
{
    public class HtmlPdfClientTest
    {
        [Fact]
        public void Ensure_Run_Error_When_NotPageConfig()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                HtmlPdfClient.Create("Teste")
                    .PageConfig(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Ensure_Run_Error_When_NotHHtml(string? html)
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
#pragma warning disable CS8604 // Possible null reference argument.
                HtmlPdfClient.Create("Teste")
                    .FromHtml(html);
#pragma warning restore CS8604 // Possible null reference argument.
            });
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Ensure_Run_Error_When_NotTemplate(string? html)
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
#pragma warning disable CS8604 // Possible null reference argument.
                HtmlPdfClient.Create("Teste")
                    .FromRazor<string>(html,"");
#pragma warning restore CS8604 // Possible null reference argument.
            });
        }

        [Theory]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        public void Ensure_Run_Error_When_InvalidLogLevel(LogLevel level)
        {
           var logger = new NullLogger<string>();

            Assert.Throws<ArgumentException>(() =>
            {
                HtmlPdfClient.Create("Teste")
                    .Logger(logger,level);
            });
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void Ensure_Run_Error_When_TimeoutNegativeOrZero(int timeout)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                HtmlPdfClient.Create("Teste")
                    .Timeout(timeout);
            });
        }

        [Fact]
        public async Task Ensure_Run_Error_When_NotHtmlSource()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await HtmlPdfClient.Create("Teste")
                    .Run((funcparam, token) =>
                    {
                        return Task.FromResult(new HtmlPdfResult<byte[]>(true,false,TimeSpan.Zero, null));
                    });
            });
        }

        [Fact]
        public async Task Ensure_RunGeneric_Error_When_NotHtmlSource()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await HtmlPdfClient.Create("Teste")
                    .Run<string,string>((funcparam, token) =>
                    {
                        return Task.FromResult(new HtmlPdfResult<string>(true, false, TimeSpan.Zero, null));
                    }, "");
            });
        }

        [Fact]
        public async Task Ensure_Run_Error_When_NotSubmmitFunction()
        {
            Func<string, CancellationToken, Task<HtmlPdfResult<byte[]>>>? submmitFunction = null;
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
#pragma warning disable CS8604 // Possible null reference argument.
                await HtmlPdfClient.Create("Teste")
                    .FromHtml("<h1>Test</h1>")
                    .Run(submmitFunction);
#pragma warning restore CS8604 // Possible null reference argument.
            });
        }


        [Fact]
        public async Task Ensure_RunGeneric_Error_When_NotSubmmitFunction()
        {
            Func<string, CancellationToken, Task<HtmlPdfResult<string>>>? submmitFunction = null;
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
#pragma warning disable CS8604 // Possible null reference argument.
                await HtmlPdfClient.Create("Teste")
                    .FromHtml("<h1>Test</h1>")
                    .Run<string,string>(submmitFunction, "");
#pragma warning restore CS8604 // Possible null reference argument.
            });
        }

        [Fact]
        public async Task Ensure_Run_Success_With_Exception_RunFunction()
        {
            var result = await HtmlPdfClient.Create("Teste")
                .FromHtml("<h1>Test</h1>")
                .Run((eventdata,token) => 
                {
                    throw new ApplicationException();
                });
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ApplicationException>(result.Error);
        }

        [Fact]
        public async Task Ensure_RunGeneric_Success_With_Exception_RunFunction()
        {
            var result = await HtmlPdfClient.Create("Teste")
                .FromHtml("<h1>Test</h1>")
                .Run<string,string>((eventdata, token) =>
                {
                    throw new ApplicationException();
                },"");
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ApplicationException>(result.Error);
        }

        [Fact]
        public async Task Ensure_Run_Success_With_Timeout_RunFunction()
        {
            var result = await HtmlPdfClient.Create("Teste")
                .FromHtml("<h1>Test</h1>")
                .Timeout(10)
                .Run((eventdata, token) =>
                {
                    Thread.Sleep(100);
                    return Task.FromResult(new HtmlPdfResult<byte[]>(true, false, TimeSpan.Zero, default));
                });
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<TimeoutException>(result.Error);
        }

        [Fact]
        public async Task Ensure_RunGeneric_Success_With_Timeout_RunFunction()
        {
            var result = await HtmlPdfClient.Create("Teste")
                .FromHtml("<h1>Test</h1>")
                .Timeout(10)
                .Run<string,string>((eventdata, token) =>
                {
                    Thread.Sleep(100);
                    return Task.FromResult(new HtmlPdfResult<string>(true,false, TimeSpan.Zero,""));
                }, "");
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<TimeoutException>(result.Error);
        }

        [Fact]
        public async Task Ensure_Run_Compress_Decompress_Request()
        {
            RequestHtmlPdf<string> request = new("x","",new(),100, null);
            var result = await HtmlPdfClient.Create("Teste")
                .PageConfig(cfg => 
                {
                    cfg.Header("<p>header</p>")
                       .Footer("<p>Footer</p>")
                       .Orientation(PageOrientation.Landscape)
                       .Format(1,2)
                       .Scale(1.4f)
                       .DisplayHeaderFooter(true)
                       .PrintBackground(false)
                       .Margins(1, 2, 3, 4);
                })
                .FromHtml("<h1>Test</h1>")
                .Timeout(10000)
                .Run((eventdata, token) =>
                {
                    request = GZipHelper.DecompressRequest<string>(eventdata);
                    return Task.FromResult(new HtmlPdfResult<byte[]>(true,false, TimeSpan.Zero, []));
                });
            Assert.Equal(10000, request.Timeout);
            Assert.Equal("Teste", request.Alias);
            Assert.Equal("<h1>Test</h1>", request.Html);
            Assert.Equal("<p>header", request.Config!.Header);
            Assert.Equal("<p>Footer", request.Config!.Footer);
            Assert.True(request.Config.DisplayHeaderFooter);
            Assert.False(request.Config.PrintBackground);
            Assert.Equal(1.4f, request.Config.Scale);
            Assert.Null(request.InputParam);
            Assert.Equal(PageOrientation.Landscape, request.Config.Orientation);
            Assert.Equal("1.0;2.0", request.Config.Size.ToString());
            Assert.Equal("1.0;2.0;3.0;4.0", request.Config.Margins.ToString());
            Assert.Equal([], result.OutputData!);
        }

        [Fact]
        public async Task Ensure_Run_Compress_Decompress_Request_with_Param()
        {
            RequestHtmlPdf<string> request = new("x","", new(),  100, null);
            var result = await HtmlPdfClient.Create("Teste")
                .PageConfig(cfg =>
                {
                    cfg.Header("<p>header</p>")
                       .Footer("<p>Footer</p>")
                       .Orientation(PageOrientation.Landscape)
                       .Format(1, 2)
                       .Scale(1.4f)
                       .DisplayHeaderFooter(true)
                       .PrintBackground(false)
                       .Margins(1, 2, 3, 4);
                })
                .FromHtml("<h1>Test</h1>")
                .Timeout(10000)
                .Run<string,string>((eventdata, token) =>
                {
                    request = GZipHelper.DecompressRequest<string>(eventdata);
                    return Task.FromResult(new HtmlPdfResult<string>(true,false, TimeSpan.Zero, "output"));
                },"input");
            Assert.Equal(10000, request.Timeout);
            Assert.Equal("Teste", request.Alias);
            Assert.Equal("<h1>Test</h1>", request.Html);
            Assert.Equal("<p>header", request.Config!.Header);
            Assert.Equal("<p>Footer", request.Config!.Footer);
            Assert.True(request.Config.DisplayHeaderFooter);
            Assert.False(request.Config.PrintBackground);
            Assert.Equal(1.4f, request.Config.Scale);
            Assert.Equal("input",request.InputParam);
            Assert.Equal(PageOrientation.Landscape, request.Config.Orientation);
            Assert.Equal("1.0;2.0", request.Config.Size.ToString());
            Assert.Equal("1.0;2.0;3.0;4.0", request.Config.Margins.ToString());
            Assert.Equal("output",result.OutputData);
        }

        [Fact]
        public void Scale_ThrowsArgumentException_WhenValueIsOutOfRange()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                HtmlPdfClient.Create("Teste")
                    .PageConfig((cfg) =>
                    {
                        cfg.Scale(0.09f);
                    });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                HtmlPdfClient.Create("Teste")
                    .PageConfig((cfg) =>
                    {
                        cfg.Scale(2.01f);
                    });
            });
        }
    }
}

