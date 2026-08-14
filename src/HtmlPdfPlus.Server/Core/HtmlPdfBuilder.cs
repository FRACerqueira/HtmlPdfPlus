// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using HtmlPdfPlus.Shared.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using NUglify;

namespace HtmlPdfPlus.Server.Core
{
    /// <summary>
    /// Builder class for configuring and creating an HTML to PDF conversion service.
    /// </summary>
    internal sealed class HtmlPdfBuilder(ILoggerFactory? loggerFactory = null) : IHtmlPdfSrvBuilder, IDisposable
    {
        private string[] _args = [];
        private byte _pagesbuffer = 5;
        private int _acquireTimeout = 5000;
        private string _sourcealias = string.Empty;
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private PdfPageConfig _pageconfig = new();
        private bool isDisposed;
        private int _recovering;
        private Func<Uri, bool> _urlAllowPolicy = DefaultUrlPolicy;
        private long _maxDecompressedRequestSize = 52_428_800;
        private readonly ConcurrentQueue<IPage> _availableBuffer = new();
        private readonly SemaphoreSlim _bufferSignal = new(0);

        /// <summary>
        /// Gets the options to disable internal features.
        /// </summary>
        public DisableOptionsHtmlToPdf DisableOptions { get; private set; } = DisableOptionsHtmlToPdf.EnabledAllFeatures;

        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        public ILogger? Log { get; private set; }

        /// <summary>
        /// Gets the log level.
        /// </summary>
        public LogLevel LevelLog { get; private set; } = LogLevel.Debug;

        /// <summary>
        /// Gets the log category name.
        /// </summary>
        public string LogCategoryName { get; private set; } = "";

        /// <inheritdoc />
        public IHtmlPdfSrvBuilder InitArguments(string? args)
        {
            if (string.IsNullOrEmpty(args))
            {
                _args = [];
            }
            else
            {
                return InitArguments(args.Split(';', StringSplitOptions.RemoveEmptyEntries));
            }
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfSrvBuilder InitArguments(string[] args)
        {
            if (args.Length == 0)
            {
                _args = [];
            }
            _args = new string[args.Length];
            args.CopyTo(_args, 0);
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfSrvBuilder PagesBuffer(byte buffer = 5)
        {
            if (buffer < 1)
            {
                throw new ArgumentException("buffer must be greater than or equal to 1");
            }
            _pagesbuffer = buffer;
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfSrvBuilder DefaultConfig(PdfPageConfig value)
        {
            _pageconfig = value;
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfSrvBuilder DefaultConfig(Action<IPdfPageConfig> config)
        {
            var cfg = new HtmlPdfConfig();
            config.Invoke(cfg);
            if (!DisableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableMinifyHtml))
            {
                if (!string.IsNullOrEmpty(cfg.PageConfig.Header))
                {
                    cfg.PageConfig.Header = Uglify.Html(cfg.PageConfig.Header).Code;
                }
                if (!string.IsNullOrEmpty(cfg.PageConfig.Footer))
                {
                    cfg.PageConfig.Footer = Uglify.Html(cfg.PageConfig.Footer).Code;
                }
            }
            _pageconfig = cfg.PageConfig;
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfSrvBuilder DisableFeatures(DisableOptionsHtmlToPdf options)
        {
            DisableOptions = options;
            if (DisableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableLogging))
            {
                Log = null;
            }
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfSrvBuilder AcquireTimeout(int value = 5000)
        {
            if (value < 10)
            {
                throw new ArgumentException("The value must be greater than or equal to 10.");
            }
            _acquireTimeout = value;
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfSrvBuilder Logger(LogLevel logLevel, string categoryName = "HtmlPdfServer")
        {
            if (logLevel is LogLevel.Critical or LogLevel.Error or LogLevel.Warning)
            {
                throw new ArgumentException($"Invalid log level {logLevel}");
            }
            if (!DisableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableLogging))
            {
                Log = loggerFactory?.CreateLogger(categoryName);
            }
            LevelLog = logLevel;
            LogCategoryName = categoryName;
            return this;
        }

        /// <inheritdoc />
        public IHtmlPdfSrvBuilder UrlAllowPolicy(Func<Uri, bool> policy)
        {
            _urlAllowPolicy = policy ?? throw new ArgumentNullException(nameof(policy), "policy is null");
            return this;
        }

        internal bool IsUrlAllowed(Uri uri) => _urlAllowPolicy(uri);

        /// <summary>
        /// Default <see cref="UrlAllowPolicy"/>: allows only http/https, and denies private,
        /// loopback and link-local address ranges - which also covers cloud metadata endpoints
        /// such as <c>169.254.169.254</c> - to close the most common SSRF vector for the
        /// <see cref="RenderMode.Url"/> render path.
        /// </summary>
        /// <remarks>
        /// This only inspects the URL when its host is already a literal IP address. A DNS
        /// hostname that resolves to a private or link-local address at connect time (Chromium
        /// does its own resolution) is not caught here - supply a DNS-aware policy via
        /// <see cref="UrlAllowPolicy"/> if that class of rebinding attack is a concern.
        /// </remarks>
        internal static bool DefaultUrlPolicy(Uri uri)
        {
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }
            if ((uri.HostNameType == UriHostNameType.IPv4 || uri.HostNameType == UriHostNameType.IPv6)
                && IPAddress.TryParse(uri.Host, out var address) && IsPrivateOrLinkLocal(address))
            {
                return false;
            }
            return true;
        }

        private static bool IsPrivateOrLinkLocal(IPAddress address)
        {
            if (IPAddress.IsLoopback(address))
            {
                return true;
            }
            var bytes = address.GetAddressBytes();
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                return bytes[0] == 10
                    || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                    || (bytes[0] == 192 && bytes[1] == 168)
                    || (bytes[0] == 169 && bytes[1] == 254);
            }
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (address.IsIPv6LinkLocal)
                {
                    return true;
                }
                return (bytes[0] & 0xFE) == 0xFC; // unique local fc00::/7
            }
            return false;
        }

        /// <inheritdoc />
        public IHtmlPdfSrvBuilder MaxDecompressedRequestSize(long value = 52_428_800)
        {
            if (value < 1)
            {
                throw new ArgumentException("The value must be greater than zero.");
            }
            _maxDecompressedRequestSize = value;
            return this;
        }

        internal long MaxDecompressedRequestSizeLimit => _maxDecompressedRequestSize;

        internal int AcquireTimeoutMs => _acquireTimeout;

        /// <summary>
        /// Gets a value indicating whether the browser is currently being restarted after an
        /// unexpected disconnect - a transient state in which the pool cannot serve pages.
        /// </summary>
        internal bool IsRecovering => Volatile.Read(ref _recovering) == 1;

        internal async Task<IHtmlPdfServer<object, byte[]>> BuildAsync(string sourcealias)
        {
            return await BuildAsync<object, byte[]>(sourcealias);
        }

        internal async Task<IHtmlPdfServer<Tin, Tout>> BuildAsync<Tin, Tout>(string sourcealias)
        {
            return await ExecuteBuildAsync<Tin, Tout>(sourcealias);
        }

        private async Task<IHtmlPdfServer<Tin, Tout>> ExecuteBuildAsync<Tin, Tout>(string sourcealias)
        {
            _sourcealias = sourcealias;
            try
            {
                _playwright = await Playwright.CreateAsync();
                if (_args.Length == 0)
                {
                    _args = ["--run-all-compositor-stages-before-draw", "--disable-dev-shm-usage", "-disable-setuid-sandbox", "--no-sandbox"];
                }
                await LaunchBrowserAsync().ConfigureAwait(false);
                LogMessage($"Build Chromium with args { string.Join("", _args) }");
                for (int i = 0; i < _pagesbuffer; i++)
                {
                    await ReplenishBufferAsync().ConfigureAwait(false);
                }
                LogMessage($"Build Chromium with buffer {_pagesbuffer}");
            }
            catch (Exception ex)
            {
                LogMessage($"Builder error: {ex}");
                throw;
            }
            return new HtmlPdfServer<Tin, Tout>(this, sourcealias);
        }

        private async Task LaunchBrowserAsync()
        {
            _browser = await _playwright!.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true, Args = _args }).ConfigureAwait(false);
            _browser.Disconnected += OnBrowserDisconnected;
        }

        /// <summary>
        /// Invoked whenever the Chromium process disconnects, whether from an intentional
        /// <see cref="Dispose"/> or from the process crashing on its own. Only the latter
        /// should trigger a relaunch, so a disposed builder is left alone.
        /// </summary>
        private void OnBrowserDisconnected(object? sender, IBrowser browser)
        {
            // Detach from the dead instance regardless of cause, so a browser that no longer
            // exists can never trigger another recovery attempt further down the line.
            browser.Disconnected -= OnBrowserDisconnected;
            if (isDisposed)
            {
                return;
            }
            LogMessage("Browser disconnected unexpectedly - attempting automatic recovery");
            _ = RecoverBrowserAsync();
        }

        /// <summary>
        /// Relaunches Chromium and refills the page pool after an unexpected disconnect,
        /// turning what used to require a manual restart into self-healing. Reentrant calls
        /// (multiple pages can fault around the same crash) are collapsed into a single attempt.
        /// </summary>
        private async Task RecoverBrowserAsync()
        {
            if (Interlocked.CompareExchange(ref _recovering, 1, 0) != 0)
            {
                return;
            }
            try
            {
                // Drain the acquire signal in lockstep with the queue, so a crash that
                // happens while pages are sitting idle in the pool doesn't leave the
                // semaphore over-counted relative to what's actually discarded below.
                while (_bufferSignal.Wait(0))
                {
                }
                while (_availableBuffer.TryDequeue(out _))
                {
                    // discard pages that belonged to the dead browser process
                }
                await LaunchBrowserAsync().ConfigureAwait(false);
                for (int i = 0; i < _pagesbuffer; i++)
                {
                    await ReplenishBufferAsync().ConfigureAwait(false);
                }
                LogMessage($"Browser recovered with buffer {_pagesbuffer}");
            }
            catch (Exception ex)
            {
                LogMessage($"Browser recovery failed: {ex}");
            }
            finally
            {
                Volatile.Write(ref _recovering, 0);
            }
        }

        internal PdfPageConfig Config => _pageconfig;

        internal int BufferLength => _availableBuffer.Count;

        /// <summary>
        /// Exposes the current browser instance for diagnostics and tests that need to
        /// observe recovery (e.g. simulating a crash and waiting for a new, connected browser).
        /// </summary>
        internal IBrowser? CurrentBrowser => _browser;

        internal async Task RestoreAvailableBuffer(IPage page)
        {
            try
            {
                await page.CloseAsync().ConfigureAwait(false);
                await ReplenishBufferAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogMessage($"RestoreAvailableBuffer Error: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Adds one freshly created page to the pool, independent of any specific page being
        /// closed. Used to restore pool capacity immediately when the page being replaced
        /// cannot be closed yet because it is still in use (see <see cref="CloseWhenSettled"/>).
        /// </summary>
        internal async Task ReplenishBufferAsync()
        {
            _availableBuffer.Enqueue(await _browser!.NewPageAsync().ConfigureAwait(false));
            _bufferSignal.Release();
            LogMessage($"RestoreAvailableBuffer to {BufferLength}");
        }

        /// <summary>
        /// Closes <paramref name="page"/> only after <paramref name="pendingWork"/> settles,
        /// instead of closing it while that work may still be running on it. Playwright's
        /// <c>PdfAsync</c> takes no cancellation token, so a caller-side timeout cannot abort it -
        /// the page must not be reused or closed until it actually finishes.
        /// </summary>
        internal void CloseWhenSettled(IPage page, Task pendingWork)
        {
            _ = pendingWork.ContinueWith(async _ =>
            {
                try
                {
                    await page.CloseAsync().ConfigureAwait(false);
                    LogMessage("Deferred close of a page completed after its timeout elapsed");
                }
                catch (Exception ex)
                {
                    LogMessage($"Deferred page close error: {ex}");
                }
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        /// <summary>
        /// Waits asynchronously for a page to become available, instead of blocking the
        /// calling thread in a synchronous polling loop. A page is only ever handed out
        /// after it has actually been enqueued, so a successful wait is always followed by
        /// a successful dequeue.
        /// </summary>
        internal async Task<IPage?> AcquireAsync(CancellationToken token)
        {
            using var ctsTimeout = new CancellationTokenSource(_acquireTimeout);
            using var acquireToken = CancellationTokenSource.CreateLinkedTokenSource(ctsTimeout.Token, token);
            try
            {
                await _bufferSignal.WaitAsync(acquireToken.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                // The caller's own token (the overall request deadline, or an external
                // cancellation) was not the cause - only this pool's own configured
                // AcquireTimeoutMs elapsed with no page freed. A genuine pool-exhaustion
                // signal. Checking the caller's token first (rather than this method's own
                // ctsTimeout) means a near-simultaneous race is resolved in the caller's
                // favor: anything caller-driven propagates instead of being misreported as
                // pool exhaustion, so the caller can classify it as a timeout/cancellation.
                LogMessage($"Not AvailableBuffer");
                return null;
            }
            _availableBuffer.TryDequeue(out var freePage);
            LogMessage($"AvailableBuffer {BufferLength}");
            return freePage;
        }

        /// <summary>
        /// Clean-up code is implemented
        /// </summary>
        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            Cleanup();
            GC.SuppressFinalize(this);
        }

        private void Cleanup()
        {
            _browser?.CloseAsync();
            _playwright?.Dispose();
        }

        private void LogMessage(string message)
        {
            if (Log is null || (!Log?.IsEnabled(LevelLog) ?? false)) return;

            switch (LevelLog)
            {
                case LogLevel.None:
                    return;
                case LogLevel.Trace:
                    logMessageForTrc(Log!, _sourcealias, message, null);
                    break;
                case LogLevel.Information:
                    logMessageForInf(Log!, _sourcealias, message, null);
                    break;
                case LogLevel.Debug:
                    logMessageForDbg(Log!, _sourcealias, message, null);
                    break;
            }
        }

        // Reusable logging
        private static readonly Action<ILogger, string, string, Exception?> logMessageForInf = LoggerMessage.Define<string, string>(LogLevel.Information, 0, "HtmlPdfBuilder({Source}) : {Message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForTrc = LoggerMessage.Define<string, string>(LogLevel.Trace, 0, "HtmlPdfBuilder({Source}) : {Message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForDbg = LoggerMessage.Define<string, string>(LogLevel.Debug, 0, "HtmlPdfBuilder({Source}) : {Message}");
    }
}
