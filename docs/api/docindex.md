![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### HtmlPdfPlus Documentation
</br>

### HtmlPdfPlus namespace

| public type | description |
| --- | --- |
| static class [HtmlPdfClient](./assemblies/HtmlPdfPlus/HtmlPdfClient.md) | Fluent interface commands to perform client HTML to PDF conversion |
| class [HtmlPdfResult&lt;T&gt;](./assemblies/HtmlPdfPlus/HtmlPdfResult-1.md) | Result of converting Html to PDF |
| class [PageMargins](./assemblies/HtmlPdfPlus/PageMargins.md) | Page margins. |
| class [PageSize](./assemblies/HtmlPdfPlus/PageSize.md) | Page size for PDF. |
| class [PdfPageConfig](./assemblies/HtmlPdfPlus/PdfPageConfig.md) | The Config PDF page. |
| class [RequestHtmlPdf&lt;T&gt;](./assemblies/HtmlPdfPlus/RequestHtmlPdf-1.md) | Request data to convert Html to PDF |
| interface [IHtmlPdfClient](./assemblies/HtmlPdfPlus/IHtmlPdfClient.md) | Fluent interface commands to HtmlPdfClientInstance. |
| interface [IHtmlPdfServer&lt;TIn,TOut&gt;](./assemblies/HtmlPdfPlus/IHtmlPdfServer-2.md) | Fluent interface commands to perform HTML to PDF conversion. |
| interface [IHtmlPdfSrvBuilder](./assemblies/HtmlPdfPlus/IHtmlPdfSrvBuilder.md) | Fluent interface commands to set instance of Chromium serverless browser. |
| enum [PageOrientation](./assemblies/HtmlPdfPlus/PageOrientation.md) | Orientation Page PDF |
| [Flags] enum [DisableOptionsHtmlToPdf](./assemblies/HtmlPdfPlus/DisableOptionsHtmlToPdf.md) | Options for disable internal features |
| enum [RenderMode](./assemblies/HtmlPdfPlus/RenderMode.md) | Explicit declaration of how RequestHtmlPdf&lt;T&gt;.Html must be interpreted by the server (literal HTML or a URL to navigate to) |
| class [ErrorInfo](./assemblies/HtmlPdfPlus/ErrorInfo.md) | Structured, serializable description of a HtmlPdfResult&lt;T&gt; failure |
| enum [ErrorCode](./assemblies/HtmlPdfPlus/ErrorCode.md) | Stable, language-agnostic classification for a HtmlPdfResult&lt;T&gt; failure |
| static class [ErrorCodeHttpMapping](./assemblies/HtmlPdfPlus/ErrorCodeHttpMapping.md) | Conventional HTTP status code for each ErrorCode, for hosts that expose HtmlPdfResult&lt;T&gt; over HTTP |
| record [HtmlPdfHealthStatus](./assemblies/HtmlPdfPlus/HtmlPdfHealthStatus.md) | Readiness status of an IHtmlPdfServer&lt;TIn,TOut&gt; instance's underlying browser and page pool |

### Microsoft.Extensions.DependencyInjection namespace

| public type | description |
| --- | --- |
| static class [HostingExtensions](./assemblies/Microsoft.Extensions.DependencyInjection/HostingExtensions.md) | Provides extension methods to add and configure HtmlPdf Server in the IServiceCollection. |

### Microsoft.AspNetCore.Routing namespace

| public type | description |
| --- | --- |
| static class [HtmlPdfEndpointExtensions](./assemblies/Microsoft.AspNetCore.Routing/HtmlPdfEndpointExtensions.md) | Maps an HTTP endpoint for IHtmlPdfServer&lt;TIn,TOut&gt; directly from the library |
| static class [HtmlPdfHealthEndpointExtensions](./assemblies/Microsoft.AspNetCore.Routing/HtmlPdfHealthEndpointExtensions.md) | Maps liveness/readiness HTTP endpoints for IHtmlPdfServer&lt;TIn,TOut&gt; directly from the library |
