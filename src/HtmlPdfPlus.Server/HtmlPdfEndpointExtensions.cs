// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Globalization;
using HtmlPdfPlus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.AspNetCore.Routing
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Maps an HTTP endpoint for <see cref="IHtmlPdfServer{TIn, TOut}"/> directly from the
    /// library, so every host exposes the same request/response contract - the one an OpenAPI
    /// document generated from these endpoints actually describes - instead of each host
    /// hand-rolling its own <c>MapPost</c> and response shaping.
    /// </summary>
    public static class HtmlPdfEndpointExtensions
    {
        /// <summary>
        /// Maps the HTML to PDF conversion endpoint for the default <c>IHtmlPdfServer&lt;object, byte[]&gt;</c>
        /// service registered via <c>AddHtmlPdfService</c>.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <param name="pattern">The route pattern. Defaults to <c>/GeneratePdf</c>.</param>
        /// <returns>A <see cref="RouteHandlerBuilder"/> for further customization (authorization, filters, etc).</returns>
        public static RouteHandlerBuilder MapHtmlPdfEndpoints(this IEndpointRouteBuilder endpoints, string pattern = "/GeneratePdf")
        {
            return MapHtmlPdfEndpoints<object, byte[]>(endpoints, pattern);
        }

        /// <summary>
        /// Maps the HTML to PDF conversion endpoint for an <c>IHtmlPdfServer&lt;object, TOut&gt;</c> service,
        /// optionally transforming the generated PDF into <typeparamref name="TOut"/> via <paramref name="afterPdf"/>.
        /// </summary>
        /// <typeparam name="TOut">The type of the output result.</typeparam>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <param name="pattern">The route pattern. Defaults to <c>/GeneratePdf</c>.</param>
        /// <param name="afterPdf">An optional function to transform the generated PDF bytes into <typeparamref name="TOut"/>.</param>
        /// <returns>A <see cref="RouteHandlerBuilder"/> for further customization (authorization, filters, etc).</returns>
        public static RouteHandlerBuilder MapHtmlPdfEndpoints<TOut>(
            this IEndpointRouteBuilder endpoints,
            string pattern = "/GeneratePdf",
            Func<byte[]?, object?, CancellationToken, Task<TOut>>? afterPdf = null)
        {
            return MapHtmlPdfEndpoints<object, TOut>(endpoints, pattern, null, afterPdf);
        }

        /// <summary>
        /// Maps the HTML to PDF conversion endpoint for an <c>IHtmlPdfServer&lt;TIn, TOut&gt;</c> service.
        /// A successful <c>byte[]</c> output is served as the raw PDF (<c>application/pdf</c>); any other
        /// <typeparamref name="TOut"/> is served as JSON. A failed conversion is served as the structured
        /// <see cref="ErrorInfo"/> contract, with the HTTP status mapped from its <see cref="ErrorCode"/>
        /// (see <see cref="ErrorCodeHttpMapping.ToHttpStatusCode"/>).
        /// </summary>
        /// <typeparam name="TIn">The type of the input parameter, carried in the client's request payload.</typeparam>
        /// <typeparam name="TOut">The type of the output result.</typeparam>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <param name="pattern">The route pattern. Defaults to <c>/GeneratePdf</c>.</param>
        /// <param name="beforePdf">An optional function to enrich the HTML/URL before conversion.</param>
        /// <param name="afterPdf">An optional function to transform the generated PDF bytes into <typeparamref name="TOut"/>.</param>
        /// <returns>A <see cref="RouteHandlerBuilder"/> for further customization (authorization, filters, etc).</returns>
        public static RouteHandlerBuilder MapHtmlPdfEndpoints<TIn, TOut>(
            this IEndpointRouteBuilder endpoints,
            string pattern = "/GeneratePdf",
            Func<string, TIn?, CancellationToken, Task<string>>? beforePdf = null,
            Func<byte[]?, TIn?, CancellationToken, Task<TOut>>? afterPdf = null)
        {
            // Bound as a raw Stream (not [FromBody] byte[]) so the request body is exactly the
            // client's bytes - gzip-compressed JSON, or plain JSON when compression is disabled -
            // with no base64/JSON-string wrapping layered on top purely because byte[] model
            // binding defaults to that. See HtmlPdfClientInstance.CreateHttpContent for the
            // matching client-side change.
            var route = endpoints.MapPost(pattern, async ([FromServices] IHtmlPdfServer<TIn, TOut> pdfServer, Stream body, CancellationToken token) =>
            {
                using var ms = new MemoryStream();
                await body.CopyToAsync(ms, token).ConfigureAwait(false);
                var context = pdfServer.ScopeRequest(ms.ToArray());
                if (beforePdf is not null)
                {
                    context = context.BeforePDF(beforePdf);
                }
                if (afterPdf is not null)
                {
                    context = context.AfterPDF(afterPdf);
                }
                var result = await context.Run(token).ConfigureAwait(false);
                return ToHttpResult(result);
            });

            // Both labels are accepted since the handler only reads the raw stream regardless of
            // Content-Type - "octet-stream" for the default gzip body, "json" for the readable,
            // uncompressed body a caller gets from DisableOptionsHtmlToPdf.DisableCompress (or from
            // a curl/manual request for debugging).
            route.Accepts<byte[]>("application/octet-stream", "application/json");
            if (typeof(TOut) == typeof(byte[]))
            {
                route.Produces<byte[]>(StatusCodes.Status200OK, "application/pdf");
            }
            else
            {
                route.Produces<HtmlPdfResult<TOut>>(StatusCodes.Status200OK);
            }
            return route
                .Produces<ErrorInfo>(StatusCodes.Status400BadRequest)
                .Produces<ErrorInfo>(StatusCodes.Status500InternalServerError)
                .Produces<ErrorInfo>(StatusCodes.Status502BadGateway)
                .Produces<ErrorInfo>(StatusCodes.Status503ServiceUnavailable)
                .Produces<ErrorInfo>(StatusCodes.Status504GatewayTimeout);
        }

        private static IResult ToHttpResult<TOut>(HtmlPdfResult<TOut> result)
        {
            if (!result.IsSuccess)
            {
                return new ErrorResult(result.Error!);
            }
            if (typeof(TOut) == typeof(byte[]))
            {
                return Results.File((byte[])(object)result.OutputData!, "application/pdf");
            }
            return Results.Json(result);
        }

        /// <summary>
        /// Writes <see cref="ErrorInfo"/> as the JSON body with the status mapped from its
        /// <see cref="ErrorCode"/>, and - when <see cref="ErrorInfo.RetryAfterSeconds"/> is set -
        /// the standard <c>Retry-After</c> header, so a backpressure signal like
        /// <see cref="ErrorCode.PoolExhausted"/> is actionable by any HTTP client, not just one
        /// that parses the JSON body.
        /// </summary>
        private sealed class ErrorResult(ErrorInfo error) : IResult
        {
            public Task ExecuteAsync(HttpContext httpContext)
            {
                if (error.RetryAfterSeconds is int seconds)
                {
                    httpContext.Response.Headers[HeaderNames.RetryAfter] = seconds.ToString(CultureInfo.InvariantCulture);
                }
                return Results.Json(error, statusCode: error.Code.ToHttpStatusCode()).ExecuteAsync(httpContext);
            }
        }
    }
}
