// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Diagnostics;
using HtmlPdfPlus;
using HtmlPdfPlus.Server.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Provides extension methods to add and configure HtmlPdf Server in the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class HostingExtensions
    {
        /// <summary>
        /// Gets the required service of type <see cref="IHtmlPdfServer{Tin, Tout}"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="provider">The service provider.</param>
        /// <returns>A service object of type <see cref="IHtmlPdfServer{Tin, Tout}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the service provider is null.</exception>
        public static IHtmlPdfServer<object, byte[]> GetHtmlPdfService(this IServiceProvider provider)
        {
            return GetHtmlPdfService<object, byte[]>(provider);
        }

        /// <summary>
        /// Gets the required service of type <see cref="IHtmlPdfServer{Tin, Tout}"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="TOut">The type of the output parameter.</typeparam>
        /// <param name="provider">The service provider.</param>
        /// <returns>A service object of type <see cref="IHtmlPdfServer{Tin, Tout}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the service provider is null.</exception>
        public static IHtmlPdfServer<object, TOut> GetHtmlPdfService<TOut>(this IServiceProvider provider)
        {
            return GetHtmlPdfService<object, TOut>(provider);
        }

        /// <summary>
        /// Gets the required service of type <see cref="IHtmlPdfServer{TIn, TOut}"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="TIn">The type of the input parameter.</typeparam>
        /// <typeparam name="TOut">The type of the output parameter.</typeparam>
        /// <param name="provider">The service provider.</param>
        /// <returns>A service object of type <see cref="IHtmlPdfServer{TIn, TOut}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the service provider is null.</exception>
        public static IHtmlPdfServer<TIn, TOut> GetHtmlPdfService<TIn, TOut>(this IServiceProvider provider)
        {
            return provider?.GetRequiredService<IHtmlPdfServer<TIn, TOut>>()
                ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Adds HtmlPdf Server to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <typeparam name="TIn">The type of the input parameter.</typeparam>
        /// <typeparam name="TOut">The type of the output parameter.</typeparam>
        /// <param name="serviceCollection">The service collection.</param>
        /// <param name="config">An action to customize HtmlPdf Server configuration.</param>
        /// <param name="sourceAlias">Alias for this instance. If empty, uses the log's CategoryName property if it exists or empty.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the service collection is null.</exception>
        public static IServiceCollection AddHtmlPdfService<TIn, TOut>(this IServiceCollection serviceCollection, Action<IHtmlPdfSrvBuilder>? config = null, string? sourceAlias = null)
        {
            sourceAlias ??= string.Empty;

            serviceCollection.AddSingleton<IHtmlPdfServer<TIn, TOut>>(service =>
            {
                var cfg = new HtmlPdfBuilder(service.GetService<ILoggerFactory>());
                config?.Invoke(cfg);
                // Resolve the final sourceAlias BEFORE creating our own default logger below - a
                // caller's own config callback may already have set up a custom-category logger
                // (rescued here from cfg.LogCategoryName), otherwise fall back to a generated,
                // guaranteed-unique alias. Every metric this builder reports is tagged with
                // sourcealias, so leaving it empty when multiple registrations share the same
                // fallback would make them indistinguishable in aggregate.
                if (string.IsNullOrEmpty(sourceAlias) && !string.IsNullOrEmpty(cfg.LogCategoryName))
                {
                    sourceAlias = cfg.LogCategoryName;
                }
                if (string.IsNullOrEmpty(sourceAlias))
                {
                    sourceAlias = $"HtmlPdfPlus-{Guid.NewGuid():N}";
                }
                if (cfg.Log is null && !cfg.DisableOptions.HasFlag(DisableOptionsHtmlToPdf.DisableLogging))
                {
                    // Same default regardless of whether a config callback was supplied - only
                    // AddHtmlPdfService() (no config) used to get this; any config callback that
                    // didn't itself call .Logger(...) silently got no logging at all. Uses the
                    // now-fully-resolved sourceAlias (not the possibly-empty parameter) as the
                    // logger's category, so a category-scoped log-level override can actually
                    // target this instance instead of always landing on an empty category.
                    cfg.Logger(LogLevel.Debug, sourceAlias);
                }
                // Disposal is primarily left to the DI container itself (it tracks and disposes
                // every IDisposable singleton it creates, including via a factory registration like
                // this one) - that happens once Dispose()/DisposeAsync() is actually called on the
                // ServiceProvider, which for the conventional app.Run()/RunAsync() hosting pattern
                // occurs after Generic Host's graceful drain, not before it (an explicit
                // ApplicationStopping hook fired too early here, tearing down the shared browser/
                // pool mid-request - see the regression test guarding that removal). Calling
                // StopAsync() alone, without ever disposing the container, does NOT trigger
                // container disposal - the ApplicationStopped registration below is a backstop for
                // exactly that gap. ApplicationStopped fires only after the graceful drain
                // completes (unlike ApplicationStopping), so it does not reintroduce the
                // mid-request teardown this design avoided; HtmlPdfBuilder.Dispose() is idempotent,
                // so running both this hook and container disposal is safe.
                var appLifetime = service.GetService<IHostApplicationLifetime>();
                appLifetime?.ApplicationStopped.Register(cfg.Dispose);
                return cfg.BuildAsync<TIn, TOut>(sourceAlias).Result;
            });
            return serviceCollection;
        }

        /// <summary>
        /// Adds HtmlPdf Server to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        /// <param name="config">An action to customize HtmlPdf Server configuration.</param>
        /// <param name="sourceAlias">Alias for this instance. If empty, uses the log's CategoryName property if it exists or empty.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
        public static IServiceCollection AddHtmlPdfService(this IServiceCollection serviceCollection, Action<IHtmlPdfSrvBuilder>? config = null, string? sourceAlias = null)
        {
            return AddHtmlPdfService<object, byte[]>(serviceCollection, config, sourceAlias);
        }

        /// <summary>
        /// Adds HtmlPdf Server to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <typeparam name="TOut">The type of the output parameter.</typeparam>
        /// <param name="serviceCollection">The service collection.</param>
        /// <param name="config">An action to customize HtmlPdf Server configuration.</param>
        /// <param name="sourceAlias">Alias for this instance. If empty, uses the log's CategoryName property if it exists or empty.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
        public static IServiceCollection AddHtmlPdfService<TOut>(this IServiceCollection serviceCollection, Action<IHtmlPdfSrvBuilder>? config = null, string? sourceAlias = null)
        {
            return AddHtmlPdfService<object, TOut>(serviceCollection, config, sourceAlias);
        }

        /// <summary>
        /// Warms up HtmlPdfServerPlus with full capacity ready.
        /// </summary>
        /// <typeparam name="TIn">The type of the input parameter.</typeparam>
        /// <typeparam name="TOut">The type of the output parameter.</typeparam>
        /// <param name="appBuild">The host application.</param>
        /// <returns>The elapsed time to warm up with full capacity ready.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not available.</exception>
        public static TimeSpan WarmupHtmlPdfService<TIn, TOut>(this IHost appBuild)
        {
            var sw = Stopwatch.StartNew();
            _ = appBuild.Services.GetRequiredService<IHtmlPdfServer<TIn, TOut>>();
            return sw.Elapsed;
        }

        /// <summary>
        /// Warms up HtmlPdfServerPlus with full capacity ready.
        /// </summary>
        /// <param name="appBuild">The host application.</param>
        /// <returns>The elapsed time to warm up with full capacity ready.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not available.</exception>
        public static TimeSpan WarmupHtmlPdfService(this IHost appBuild)
        {
            return WarmupHtmlPdfService<object, byte[]>(appBuild);
        }

        /// <summary>
        /// Warms up HtmlPdfServerPlus with full capacity ready.
        /// </summary>
        /// <typeparam name="TOut">The type of the output parameter.</typeparam>
        /// <param name="appBuild">The host application.</param>
        /// <returns>The elapsed time to warm up with full capacity ready.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not available.</exception>
        public static TimeSpan WarmupHtmlPdfService<TOut>(this IHost appBuild)
        {
            return WarmupHtmlPdfService<object, TOut>(appBuild);
        }
    }
}
