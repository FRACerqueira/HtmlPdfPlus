// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using HtmlPdfPlus.Server.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace TestHtmlPdfPlus.HtmlPdfSrvPlus
{
#pragma warning disable IDE0079
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
    public class HtmlPdfBuilderTest
    {

        [Fact]
        public void Ensure_Create_Error_When_InvalidPageBuffer()
        {
            IHtmlPdfSrvBuilder obj = new HtmlPdfBuilder();
            Assert.Throws<ArgumentException>(() =>
            {

                obj.PagesBuffer(0);
            });
            ((IDisposable)obj).Dispose();
        }

        [Theory]
        [InlineData(9)]
        [InlineData(501)]
        public void Ensure_Create_Error_When_InvalidAcquireWaitTime(int wait)
        {

            IHtmlPdfSrvBuilder obj = new HtmlPdfBuilder();
            Assert.Throws<ArgumentException>(() =>           
            {
                obj.AcquireWaitTime(wait);
            });
            ((IDisposable)obj).Dispose();
        }

        [Theory]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        public void Ensure_Run_Error_When_InvalidLogLevel(LogLevel level)
        {
            var loggerfact = NullLoggerFactory.Instance;

            IHtmlPdfSrvBuilder obj = new HtmlPdfBuilder(loggerfact);
            Assert.Throws<ArgumentException>(() =>
            {
                obj.Logger(level);
            });
            ((IDisposable)obj).Dispose();
        }

        [Fact]
        public async Task Ensure_buid_With_DefaultBuffer()
        {
            using var obj = new HtmlPdfBuilder();
            await obj.BuildAsync("Teste");
            Assert.Equal(5, obj.BufferLength);
        }

        [Fact]
        public async Task Ensure_buid_With_CustomBuffer()
        {
            using var obj = new HtmlPdfBuilder();
            obj.InitArguments("--disable-dev-shm-usage;-no-first-run");
            obj.PagesBuffer(1);
            await obj.BuildAsync("Teste");
            Assert.Equal(1, obj.BufferLength);
        }


        [Fact]
        public async Task Ensure_buid_With_AccquireBuffer()
        {
            using var obj = new HtmlPdfBuilder(null);
            using var cts = new CancellationTokenSource();
            await obj.BuildAsync("Teste");
            cts.CancelAfter(100);
            obj.Acquire(cts.Token);
            Assert.Equal(4, obj.BufferLength);
        }

        [Fact]
        public async Task Ensure_buid_With_AccquireTimeout()
        {
            using var obj = new HtmlPdfBuilder();
            obj.AcquireWaitTime(10);
            obj.AcquireTimeout(20);
            using var cts = new CancellationTokenSource();
            obj.PagesBuffer(1);
            await obj.BuildAsync("Teste");
            cts.CancelAfter(200);
            var firtpage = obj.Acquire(cts.Token);
            var page = obj.Acquire(CancellationToken.None);
            Assert.NotNull(firtpage);
            Assert.Null(page);
        }

        [Fact]
        public async Task Ensure_buid_With_NotBufferExternalTimeout()
        {
            using var obj = new HtmlPdfBuilder();
            using var cts = new CancellationTokenSource();
            obj.PagesBuffer(1);
            await obj.BuildAsync("Teste");
            cts.CancelAfter(200);
            var firtpage = obj.Acquire(cts.Token);
            var page = obj.Acquire(cts.Token);
            Assert.NotNull(firtpage);
            Assert.Null(page);
        }


        [Fact]
        public async Task Ensure_buid_With_RestoreAvailableBuffer()
        {
            using var obj = new HtmlPdfBuilder();
            using var cts = new CancellationTokenSource();
            await obj.BuildAsync("Teste");
            cts.CancelAfter(100);
            var page = obj.Acquire(cts.Token);
            if (page is not null)
            {
                await obj.RestoreAvailableBuffer(page);
            }
            Assert.Equal(5, obj.BufferLength);
        }

        [Theory]
        [InlineData("http://10.0.0.1", false)]
        [InlineData("http://172.16.5.4", false)]
        [InlineData("http://172.32.5.4", true)]
        [InlineData("http://192.168.1.1", false)]
        [InlineData("http://169.254.169.254", false)]
        [InlineData("http://127.0.0.1", false)]
        [InlineData("http://8.8.8.8", true)]
        [InlineData("http://example.com", true)]
        [InlineData("https://example.com", true)]
        [InlineData("ftp://example.com", false)]
        [InlineData("http://[::1]", false)]
        [InlineData("http://[fe80::1]", false)]
        [InlineData("http://[fc00::1]", false)]
        [InlineData("http://[2001:4860:4860::8888]", true)]
        public void Ensure_DefaultUrlPolicy_ClassifiesUrl(string url, bool expectedallowed)
        {
            Assert.Equal(expectedallowed, HtmlPdfBuilder.DefaultUrlPolicy(new Uri(url)));
        }

        [Fact]
        public void Ensure_UrlAllowPolicy_ThrowsArgumentNullException_WhenPolicyIsNull()
        {
            IHtmlPdfSrvBuilder obj = new HtmlPdfBuilder();
            Assert.Throws<ArgumentNullException>(() => obj.UrlAllowPolicy(null!));
            ((IDisposable)obj).Dispose();
        }

        [Fact]
        public void Ensure_UrlAllowPolicy_OverridesDefault()
        {
            using var obj = new HtmlPdfBuilder();
            var deniedByDefault = new Uri("http://169.254.169.254");
            Assert.False(obj.IsUrlAllowed(deniedByDefault));

            obj.UrlAllowPolicy(_ => true);

            Assert.True(obj.IsUrlAllowed(deniedByDefault));
        }
    }
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
#pragma warning restore IDE0079
}

