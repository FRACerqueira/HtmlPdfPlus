// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Collections.Concurrent;
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
        private int _acquireWaitTime = 10;
        private int _acquireTimeout = 5000;
        private string _sourcealias = string.Empty;
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private PdfPageConfig _pageconfig = new();
        private bool isDisposed;
        private readonly ConcurrentQueue<IPage> _availableBuffer = new();

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
        public IHtmlPdfSrvBuilder AcquireWaitTime(int value = 50)
        {
            if (value < 10 || value > 500)
            {
                throw new ArgumentException("The value amount must be between 10 and 500.");
            }
            _acquireWaitTime = value;
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

        internal async Task<IHtmlPdfServer<object, byte[]>> BuildAsync(string sourcealias)
        {
            return await BuildAsync<object, byte[]>(sourcealias).ConfigureAwait(false);
        }

        internal async Task<IHtmlPdfServer<Tin, Tout>> BuildAsync<Tin, Tout>(string sourcealias)
        {
            return await ExecuteBuildAsync<Tin, Tout>(sourcealias).ConfigureAwait(false);
        }

        private async Task<IHtmlPdfServer<Tin, Tout>> ExecuteBuildAsync<Tin, Tout>(string sourcealias)
        {
            _sourcealias = sourcealias;
            try
            {
                _playwright = await Playwright.CreateAsync().ConfigureAwait(false);
                if (_args.Length == 0)
                {
                    _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true, Args = ["--run-all-compositor-stages-before-draw", "--disable-dev-shm-usage", "-disable-setuid-sandbox", "--no-sandbox"] }).ConfigureAwait(false);
                }
                else
                {
                    _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true, Args = _args }).ConfigureAwait(false);
                    LogMessage($"Build Chromium with args {_args}");
                }
                for (int i = 0; i < _pagesbuffer; i++)
                {
                    _availableBuffer.Enqueue(await _browser.NewPageAsync().ConfigureAwait(false));
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

        internal PdfPageConfig Config => _pageconfig;

        internal int BufferLength => _availableBuffer.Count;

        internal async Task RestoreAvailableBuffer(IPage page)
        {
            try
            {
                await page.CloseAsync().ConfigureAwait(false);
                _availableBuffer.Enqueue(await _browser!.NewPageAsync().ConfigureAwait(false));
                LogMessage($"RestoreAvailableBuffer to {BufferLength}");
            }
            catch (Exception ex)
            {
                LogMessage($"RestoreAvailableBuffer Error: {ex}");
                throw;
            }
        }

        internal IPage? Acquire(CancellationToken token)
        {
            using var ctsTimeout = new CancellationTokenSource();
            ctsTimeout.CancelAfter(_acquireTimeout);
            using var acquireToken = CancellationTokenSource.CreateLinkedTokenSource(ctsTimeout.Token, token);
            while (!acquireToken.IsCancellationRequested)
            {
                if (_availableBuffer.TryDequeue(out var freePage))
                {
                    LogMessage($"AvailableBuffer {BufferLength}");
                    return freePage;
                }
                acquireToken.Token.WaitHandle.WaitOne(_acquireWaitTime);
            }
            LogMessage($"Not AvailableBuffer");
            return null;
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
        private static readonly Action<ILogger, string, string, Exception?> logMessageForInf = LoggerMessage.Define<string, string>(LogLevel.Information, 0, "HtmlPdfBuilder({source}) : {message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForTrc = LoggerMessage.Define<string, string>(LogLevel.Trace, 0, "HtmlPdfBuilder({source}) : {message}");
        private static readonly Action<ILogger, string, string, Exception?> logMessageForDbg = LoggerMessage.Define<string, string>(LogLevel.Debug, 0, "HtmlPdfBuilder({source}) : {message}");
    }
}
