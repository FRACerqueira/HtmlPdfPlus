// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
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
        private int _isDisposed;
        private int _recovering;
        private int _poolStarved;
        private int _browserGeneration;
        private Func<Uri, bool> _urlAllowPolicy = DefaultUrlPolicy;
        private long _maxDecompressedRequestSize = 52_428_800;
        private readonly ConcurrentQueue<IPage> _availableBuffer = new();
        private readonly SemaphoreSlim _bufferSignal = new(0);

        // Tracks which browser instance (by generation number, bumped on every successful
        // relaunch) each live page was created from, so a page checked out before a crash - or
        // still sitting in the pool in the narrow window before RecoverBrowserAsync's drain runs -
        // can be told apart from a page belonging to the current browser instead of being handed
        // out, or replenished into the pool, as if it still were one.
        private readonly ConcurrentDictionary<IPage, int> _pageGenerations = new();

        // Backoff between relaunch attempts inside RecoverBrowserAsync: up to 2 retries after the
        // first failed attempt (3 attempts total) before giving up and staying degraded. Worst
        // case is bounded by BrowserLaunchTimeout PER ATTEMPT, not just the backoff itself: 3 x 30s
        // launch timeout + 3s of backoff = ~93s.
        private static readonly TimeSpan[] RecoveryBackoff =
        [
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2)
        ];

        // A Meter of its own (rather than the shared static one in HtmlPdfMetrics) so the
        // ObservableGauge below - whose callback closes over `this` - can be unregistered by
        // disposing just this instance's Meter, without disposing the process-wide Counter/
        // Histogram every other HtmlPdfBuilder instance also reports through.
        private readonly Meter _instanceMeter = new(HtmlPdfMetrics.MeterName);

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

        /// <summary>
        /// Gets a value indicating whether the most recent recovery completed with a connected
        /// browser but zero usable pages - a state that, unlike a momentarily empty pool under
        /// normal load, cannot self-correct on its own. See <see cref="HtmlPdfHealthStatus.PoolStarved"/>.
        /// </summary>
        internal bool IsPoolStarved => Volatile.Read(ref _poolStarved) == 1;

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
            // Observed lazily by whatever metrics backend the host wires up (see
            // HtmlPdfMetrics) - tagged with sourcealias so multiple AddHtmlPdfService
            // registrations remain distinguishable.
            _instanceMeter.CreateObservableGauge(
                "htmlpdfplus.pool.available_pages",
                () => new Measurement<int>(BufferLength, new KeyValuePair<string, object?>("sourcealias", _sourcealias)),
                description: "Number of pages currently available in the pool.");
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

        // Bounds Chromium.LaunchAsync (cold start and every recovery attempt) so a stuck launch
        // fails fast and diagnosably instead of hanging the DI factory's blocking .Result call, or
        // a recovery attempt, indefinitely.
        private static readonly TimeSpan BrowserLaunchTimeout = TimeSpan.FromSeconds(30);

        private async Task LaunchBrowserAsync()
        {
            var launchTask = _playwright!.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true, Args = _args });
            var completed = await Task.WhenAny(launchTask, Task.Delay(BrowserLaunchTimeout)).ConfigureAwait(false);
            if (completed != launchTask)
            {
                // Observe a fault from the abandoned launch (if it ever settles) instead of
                // leaving it unobserved; it can no longer become `_browser` either way.
                _ = launchTask.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        LogMessage($"Abandoned browser launch (past {BrowserLaunchTimeout}) faulted: {t.Exception}");
                    }
                    else if (t.IsCompletedSuccessfully)
                    {
                        LogMessage($"Abandoned browser launch (past {BrowserLaunchTimeout}) eventually succeeded - closing it, it was never adopted");
                        _ = t.Result.CloseAsync();
                    }
                }, TaskScheduler.Default);
                throw new TimeoutException($"Chromium did not launch within {BrowserLaunchTimeout}.");
            }
            _browser = await launchTask.ConfigureAwait(false);
            // Bumped here, immediately adjacent to the _browser assignment with no intervening
            // await, instead of later in RecoverBrowserAsync after the drain/refill work - a
            // concurrent ReplenishBufferAsync reading _browser and _browserGeneration together
            // (from a request returning its page) could otherwise observe the new browser paired
            // with the still-old generation number during that gap, and mistag a brand-new,
            // perfectly valid page as belonging to a stale generation.
            Interlocked.Increment(ref _browserGeneration);
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
            if (Volatile.Read(ref _isDisposed) == 1)
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
        /// A relaunch that fails is retried with backoff (see <see cref="RecoveryBackoff"/>)
        /// instead of leaving the pool permanently unrecoverable after one transient failure.
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
                // AcquireAsync's own generation check is the correctness backstop for whatever
                // this best-effort drain still races past (see its comments).
                while (_bufferSignal.Wait(0))
                {
                }
                while (_availableBuffer.TryDequeue(out var stale))
                {
                    _pageGenerations.TryRemove(stale, out _);
                }

                Exception? lastLaunchError = null;
                for (var attempt = 0; attempt <= RecoveryBackoff.Length; attempt++)
                {
                    if (Volatile.Read(ref _isDisposed) == 1)
                    {
                        LogMessage("Recovery aborted - builder disposed mid-recovery");
                        return;
                    }
                    try
                    {
                        await LaunchBrowserAsync().ConfigureAwait(false);
                        lastLaunchError = null;
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastLaunchError = ex;
                        LogMessage($"Browser relaunch attempt {attempt + 1}/{RecoveryBackoff.Length + 1} failed: {ex}");
                        if (attempt < RecoveryBackoff.Length)
                        {
                            await Task.Delay(RecoveryBackoff[attempt]).ConfigureAwait(false);
                        }
                    }
                }
                if (lastLaunchError is not null)
                {
                    // Every attempt failed - stay degraded (GetHealthStatus already reports this
                    // honestly, since CurrentBrowser still points at the old, disconnected
                    // instance) rather than retrying forever.
                    throw lastLaunchError;
                }

                if (Volatile.Read(ref _isDisposed) == 1)
                {
                    // Disposed while the (ultimately successful) relaunch was in flight -
                    // Cleanup() already ran and closed whatever `_browser` referenced AT THAT
                    // TIME, which may have been the old dead instance rather than this newly
                    // launched one. Close it explicitly instead of leaking the process.
                    LogMessage("Recovery completed after disposal - closing the orphaned browser");
                    await _browser!.CloseAsync().ConfigureAwait(false);
                    return;
                }

                // Generation already bumped inside LaunchBrowserAsync, immediately adjacent to the
                // _browser assignment - see its comment.
                HtmlPdfMetrics.BrowserRestarts.Add(1, new KeyValuePair<string, object?>("sourcealias", _sourcealias));

                var refilled = 0;
                for (int i = 0; i < _pagesbuffer; i++)
                {
                    try
                    {
                        await ReplenishBufferAsync().ConfigureAwait(false);
                        refilled++;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Pool refill stopped after {refilled}/{_pagesbuffer} pages: {ex}");
                        break;
                    }
                }
                if (refilled == 0)
                {
                    // The browser came back but not a single page could be created - unlike a
                    // momentarily empty pool under normal load, this cannot self-correct (no
                    // pages means no request can ever acquire one to later return one). Surface
                    // it as unhealthy instead of falsely reporting readiness, and log it above
                    // Information so it survives a host's default minimum level even though this
                    // instance's own configured LevelLog may be lower.
                    Volatile.Write(ref _poolStarved, 1);
                    LogMessage($"Browser recovered but the pool could not be refilled (0/{_pagesbuffer}) - marking PoolStarved", LogLevel.Error);
                }
                else
                {
                    LogMessage($"Browser recovered with buffer {refilled}/{_pagesbuffer}");
                }
            }
            catch (Exception ex)
            {
                // Every retry attempt failed - this is the final, unrecoverable outcome for this
                // crash (see RecoveryBackoff), so it is logged above Information regardless of
                // this instance's own configured LevelLog, the same way the PoolStarved case above
                // is - otherwise it can be entirely invisible under a host's default minimum level.
                LogMessage($"Browser recovery failed: {ex}", LogLevel.Error);
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

        /// <summary>
        /// Exposes whether <see cref="Dispose"/> has already run, for tests that need to observe
        /// disposal timing deterministically without waiting on the async browser-close side effect.
        /// </summary>
        internal bool IsDisposed => Volatile.Read(ref _isDisposed) == 1;

        /// <summary>
        /// Returns a page to the pool after a request finishes with it: closes it, and - unless it
        /// belonged to a browser generation that has since crashed and been replaced (in which case
        /// recovery's own refill already restored capacity, so adding another page here would
        /// silently overshoot PagesBuffer) - replenishes the pool with a fresh one. Never throws:
        /// this runs from a request's own `finally` block, so a pool-bookkeeping failure here must
        /// never override whatever outcome the request itself already produced (a success, or an
        /// unrelated failure).
        /// </summary>
        internal async Task RestoreAvailableBuffer(IPage page)
        {
            try
            {
                await page.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Closing a page from an already-dead browser is expected to no-op, not throw
                // (verified empirically) - a throw here means some OTHER cause (network hiccup,
                // an abrupt process kill leaving the transport in a different state). Log it, but
                // don't let it skip the generation cleanup below or override the caller's result.
                LogMessage($"RestoreAvailableBuffer: page.CloseAsync failed: {ex}", LogLevel.Warning);
            }

            var isCurrentGeneration = _pageGenerations.TryGetValue(page, out var pageGeneration)
                && pageGeneration == Volatile.Read(ref _browserGeneration);
            _pageGenerations.TryRemove(page, out _);
            if (!isCurrentGeneration)
            {
                return;
            }
            await TryReplenishBufferAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Best-effort variant of <see cref="ReplenishBufferAsync"/> for the request-return paths
        /// (<see cref="RestoreAvailableBuffer"/> and the stuck-render branch in
        /// <c>HtmlPdfServer.GeneratePDF</c>'s `finally`), where a failure must never propagate and
        /// override whatever outcome the request itself already produced. Logs the failure and
        /// marks the pool starved if it leaves zero pages available, instead of throwing.
        /// </summary>
        internal async Task TryReplenishBufferAsync()
        {
            try
            {
                await ReplenishBufferAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogMessage($"Pool replenish failed on request return: {ex}", LogLevel.Error);
                if (BufferLength == 0)
                {
                    Volatile.Write(ref _poolStarved, 1);
                }
            }
        }

        /// <summary>
        /// Adds one freshly created page to the pool, independent of any specific page being
        /// closed. Used to restore pool capacity immediately when the page being replaced
        /// cannot be closed yet because it is still in use (see <see cref="CloseWhenSettled"/>).
        /// </summary>
        internal async Task ReplenishBufferAsync()
        {
            var page = await _browser!.NewPageAsync().ConfigureAwait(false);
            _pageGenerations[page] = Volatile.Read(ref _browserGeneration);
            _availableBuffer.Enqueue(page);
            _bufferSignal.Release();
            // Any successful add proves the pool isn't stuck starved anymore (see IsPoolStarved).
            Volatile.Write(ref _poolStarved, 0);
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
                finally
                {
                    // The continuation's own parameter is already named `_`, so an `out _`
                    // discard here would resolve to that Task instead of a real discard.
                    _pageGenerations.TryRemove(page, out var removedGeneration);
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
            var waitSw = Stopwatch.StartNew();
            using var ctsTimeout = new CancellationTokenSource(_acquireTimeout);
            using var acquireToken = CancellationTokenSource.CreateLinkedTokenSource(ctsTimeout.Token, token);
            while (true)
            {
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
                    RecordAcquireWait(waitSw, "pool_exhausted");
                    return null;
                }
                catch (OperationCanceledException)
                {
                    // Caller-driven (overall deadline or external cancellation) - still real time
                    // spent waiting on pool capacity, worth recording, but must propagate unchanged
                    // so RunServer can classify it as Timeout/Canceled.
                    RecordAcquireWait(waitSw, "canceled");
                    throw;
                }
                if (!_availableBuffer.TryDequeue(out var freePage))
                {
                    // A concurrent RecoverBrowserAsync drained the queue between our successful
                    // wait and this dequeue - the permit we just consumed has no matching entry
                    // anymore. Wait again within the same overall acquire budget instead of
                    // misreporting this as pool exhaustion for a page that never existed.
                    continue;
                }
                if (_pageGenerations.TryGetValue(freePage, out var pageGeneration)
                    && pageGeneration != Volatile.Read(ref _browserGeneration))
                {
                    // A page from a browser generation that has since crashed and been replaced.
                    // Discard it (best-effort; it belongs to an already-dead browser, so the
                    // close is expected to no-op rather than throw) and keep waiting, instead of
                    // handing out a page whose underlying browser no longer exists.
                    _pageGenerations.TryRemove(freePage, out _);
                    _ = freePage.CloseAsync();
                    continue;
                }
                LogMessage($"AvailableBuffer {BufferLength}");
                RecordAcquireWait(waitSw, "acquired");
                return freePage;
            }
        }

        /// <summary>
        /// Records the acquire-wait metric (see <see cref="HtmlPdfMetrics.AcquireWaitDuration"/>),
        /// tagged with this instance's source alias and how the wait ended.
        /// </summary>
        private void RecordAcquireWait(Stopwatch waitSw, string outcome)
        {
            HtmlPdfMetrics.AcquireWaitDuration.Record(
                waitSw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("sourcealias", _sourcealias),
                new KeyValuePair<string, object?>("outcome", outcome));
        }

        /// <summary>
        /// Clean-up code is implemented
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;
            Cleanup();
            GC.SuppressFinalize(this);
        }

        private void Cleanup()
        {
            _instanceMeter.Dispose();
            try
            {
                // Bounded, not indefinite: give the graceful browser.close() handshake a real
                // chance to finish before the Playwright driver connection goes away immediately
                // below - previously fire-and-forget, so Chromium was more often killed than
                // cleanly closed, and any fault from this call went unobserved. Still synchronous
                // because IDisposable.Dispose() has no async form to hand this off to.
                _browser?.CloseAsync().Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                LogMessage($"Error closing browser during dispose: {ex}");
            }
            _playwright?.Dispose();
        }

        private void LogMessage(string message) => LogMessage(message, null);

        /// <summary>
        /// Logs a message at <paramref name="severity"/> if given, or at this instance's
        /// configured <see cref="LevelLog"/> otherwise. A small number of failure sites (a
        /// fully-exhausted browser recovery, a pool left starved) pass an explicit elevated
        /// severity so they have a chance to reach a sink under a host's default minimum log
        /// level, regardless of what <see cref="LevelLog"/> was configured to - the same
        /// <see cref="ILogger.IsEnabled(LogLevel)"/> gate still applies, so the host's own minimum
        /// level configuration is always respected.
        /// </summary>
        private void LogMessage(string message, LogLevel? severity)
        {
            var level = severity ?? LevelLog;
            if (Log is null || !Log.IsEnabled(level)) return;

            switch (level)
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
                case LogLevel.Warning:
                    logMessageForWrn(Log!, _sourcealias, message, null);
                    break;
                case LogLevel.Error:
                    logMessageForErr(Log!, _sourcealias, message, null);
                    break;
                case LogLevel.Critical:
                    logMessageForCrt(Log!, _sourcealias, message, null);
                    break;
            }
        }

        // Reusable logging
        private static readonly Action<ILogger, string, string, Exception?> logMessageForInf = LoggerMessage.Define<string, string>(LogLevel.Information, 0, "HtmlPdfBuilder({Source}) : {Message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForTrc = LoggerMessage.Define<string, string>(LogLevel.Trace, 0, "HtmlPdfBuilder({Source}) : {Message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForDbg = LoggerMessage.Define<string, string>(LogLevel.Debug, 0, "HtmlPdfBuilder({Source}) : {Message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForWrn = LoggerMessage.Define<string, string>(LogLevel.Warning, 0, "HtmlPdfBuilder({Source}) : {Message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForErr = LoggerMessage.Define<string, string>(LogLevel.Error, 0, "HtmlPdfBuilder({Source}) : {Message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForCrt = LoggerMessage.Define<string, string>(LogLevel.Critical, 0, "HtmlPdfBuilder({Source}) : {Message}");
    }
}
