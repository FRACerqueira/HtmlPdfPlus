![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### HtmlPdfPlus.Server assembly
</br>

### HtmlPdfPlus namespace

| public type | description |
| --- | --- |
| record [HtmlPdfHealthStatus](./HtmlPdfPlus/HtmlPdfHealthStatus.md) | Readiness status of an [`IHtmlPdfServer`](./HtmlPdfPlus/IHtmlPdfServer-2.md) instance's underlying browser and page pool, as reported by `MapHtmlPdfHealthEndpoints`. |
| interface [IHtmlPdfServer&lt;TIn,TOut&gt;](./HtmlPdfPlus/IHtmlPdfServer-2.md) | Fluent interface commands to perform HTML to PDF conversion. |
| interface [IHtmlPdfServerContext&lt;TIn,TOut&gt;](./HtmlPdfPlus/IHtmlPdfServerContext-2.md) | Fluent interface commands to input sources HTML or Url to PDF conversion. |
| interface [IHtmlPdfSrvBuilder](./HtmlPdfPlus/IHtmlPdfSrvBuilder.md) | Fluent interface commands to set instance of Chromium serverless browser. |

### Microsoft.AspNetCore.Routing namespace

| public type | description |
| --- | --- |
| static class [HtmlPdfEndpointExtensions](./Microsoft.AspNetCore.Routing/HtmlPdfEndpointExtensions.md) | Maps an HTTP endpoint for [`IHtmlPdfServer`](./HtmlPdfPlus/IHtmlPdfServer-2.md) directly from the library, so every host exposes the same request/response contract - the one an OpenAPI document generated from these endpoints actually describes - instead of each host hand-rolling its own `MapPost` and response shaping. |
| static class [HtmlPdfHealthEndpointExtensions](./Microsoft.AspNetCore.Routing/HtmlPdfHealthEndpointExtensions.md) | Maps liveness/readiness HTTP endpoints for [`IHtmlPdfServer`](./HtmlPdfPlus/IHtmlPdfServer-2.md) directly from the library, so an orchestrator (Kubernetes, etc.) can observe renderer health from outside instead of inferring it from request timeouts. |

### Microsoft.Extensions.DependencyInjection namespace

| public type | description |
| --- | --- |
| static class [HostingExtensions](./Microsoft.Extensions.DependencyInjection/HostingExtensions.md) | Provides extension methods to add and configure HtmlPdf Server in the IServiceCollection. |

### See Also
* [Main Index](../docindex.md)
