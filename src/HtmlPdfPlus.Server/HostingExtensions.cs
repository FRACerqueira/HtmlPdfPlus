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
        /// Gets the required service of type <see cref="IHtmlPdfServer{Tin, Tout}"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="Tin">The type of the input parameter.</typeparam>
        /// <typeparam name="TOut">The type of the output parameter.</typeparam>
        /// <param name="provider">The service provider.</param>
        /// <returns>A service object of type <see cref="IHtmlPdfServer{Tin, Tout}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the service provider is null.</exception>
        public static IHtmlPdfServer<Tin, TOut> GetHtmlPdfService<Tin, TOut>(this IServiceProvider provider)
        {
            return provider?.GetRequiredService<IHtmlPdfServer<Tin, TOut>>()
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
                var appLifetime = service.GetService<IHostApplicationLifetime>();
                var cfg = new HtmlPdfBuilder(service.GetService<ILoggerFactory>());
                if (config is null)
                {
                    cfg.Logger(LogLevel.Debug, sourceAlias);
                }
                else
                {
                    config.Invoke(cfg);
                }
                appLifetime?.ApplicationStopping.Register(cfg.Dispose);
                if (string.IsNullOrEmpty(sourceAlias) && !string.IsNullOrEmpty(cfg.LogCategoryName))
                {
                    sourceAlias = cfg.LogCategoryName;
                }
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
