// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using HtmlPdfPlus.Server.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.AspNetCore.Routing
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Maps liveness/readiness HTTP endpoints for <see cref="IHtmlPdfServer{Tin, Tout}"/> directly
    /// from the library, so an orchestrator (Kubernetes, etc.) can observe renderer health from
    /// outside instead of inferring it from request timeouts.
    /// </summary>
    public static class HtmlPdfHealthEndpointExtensions
    {
        /// <summary>
        /// Maps liveness/readiness endpoints for the default <c>IHtmlPdfServer&lt;object, byte[]&gt;</c>
        /// service registered via <c>AddHtmlPdfService</c>.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <param name="livePattern">The liveness route pattern. Defaults to <c>/healthz</c>.</param>
        /// <param name="readyPattern">The readiness route pattern. Defaults to <c>/readyz</c>.</param>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> for further customization.</returns>
        public static IEndpointRouteBuilder MapHtmlPdfHealthEndpoints(this IEndpointRouteBuilder endpoints, string livePattern = "/healthz", string readyPattern = "/readyz")
        {
            return MapHtmlPdfHealthEndpoints<object, byte[]>(endpoints, livePattern, readyPattern);
        }

        /// <summary>
        /// Maps liveness/readiness endpoints for an <c>IHtmlPdfServer&lt;TIn, TOut&gt;</c> service.
        /// </summary>
        /// <typeparam name="TIn">The type of the input parameter.</typeparam>
        /// <typeparam name="TOut">The type of the output result.</typeparam>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <param name="livePattern">The liveness route pattern. Defaults to <c>/healthz</c>.</param>
        /// <param name="readyPattern">The readiness route pattern. Defaults to <c>/readyz</c>.</param>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> for further customization.</returns>
        public static IEndpointRouteBuilder MapHtmlPdfHealthEndpoints<TIn, TOut>(this IEndpointRouteBuilder endpoints, string livePattern = "/healthz", string readyPattern = "/readyz")
        {
            // Liveness: the service resolved from DI without throwing means the process itself
            // is responsive. Deliberately does not inspect the browser/pool - a crashed browser
            // is a readiness concern (this instance should stop receiving traffic while it
            // recovers), not a liveness one (restarting the whole process would not help and
            // would just discard an in-progress auto-recovery).
            endpoints.MapGet(livePattern, ([FromServices] IHtmlPdfServer<TIn, TOut> pdfServer) => Results.Ok())
                .Produces(StatusCodes.Status200OK);

            // Readiness: reflects renderer health (browser connected, not mid-restart), not
            // per-request saturation - a momentarily empty pool is still healthy and should keep
            // receiving traffic, backed off via the standard PoolExhausted/Retry-After signal
            // instead of being pulled out of rotation here.
            endpoints.MapGet(readyPattern, ([FromServices] IHtmlPdfServer<TIn, TOut> pdfServer) =>
            {
                if (pdfServer is not HtmlPdfServer<TIn, TOut> concrete)
                {
                    // Only the library's own concrete IHtmlPdfServer implementation exposes
                    // builder/pool internals - a custom registration (a test double, a
                    // decorator) has no health signal to report, which is itself a readiness
                    // failure rather than a crash an orchestrator would misread as this probe
                    // endpoint being broken.
                    return Results.Json(new HtmlPdfHealthStatus(false, false, 0), statusCode: StatusCodes.Status503ServiceUnavailable);
                }
                var status = concrete.GetHealthStatus();
                return status.Healthy
                    ? Results.Ok(status)
                    : Results.Json(status, statusCode: StatusCodes.Status503ServiceUnavailable);
            })
                .Produces<HtmlPdfHealthStatus>(StatusCodes.Status200OK)
                .Produces<HtmlPdfHealthStatus>(StatusCodes.Status503ServiceUnavailable);

            return endpoints;
        }
    }
}
