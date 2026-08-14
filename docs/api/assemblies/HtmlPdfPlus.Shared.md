![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### HtmlPdfPlus.Shared assembly
</br>

### HtmlPdfPlus namespace

| public type | description |
| --- | --- |
| [Flags] enum [DisableOptionsHtmlToPdf](./HtmlPdfPlus/DisableOptionsHtmlToPdf.md) | Options for disable internal features |
| enum [ErrorCode](./HtmlPdfPlus/ErrorCode.md) | Stable, language-agnostic classification for a [`HtmlPdfResult`](./HtmlPdfPlus/HtmlPdfResult-1.md) failure. |
| static class [ErrorCodeHttpMapping](./HtmlPdfPlus/ErrorCodeHttpMapping.md) | Conventional HTTP status code for each [`ErrorCode`](./HtmlPdfPlus/ErrorCode.md), for hosts that expose [`HtmlPdfResult`](./HtmlPdfPlus/HtmlPdfResult-1.md) over HTTP and want the status line itself to carry the failure category - not just a 200 with an embedded success flag. Returns a plain Int32 so this carries no dependency on any specific web framework. |
| class [ErrorInfo](./HtmlPdfPlus/ErrorInfo.md) | Structured, serializable description of a [`HtmlPdfResult`](./HtmlPdfPlus/HtmlPdfResult-1.md) failure. |
| class [HtmlPdfResult&lt;T&gt;](./HtmlPdfPlus/HtmlPdfResult-1.md) | Result of converting Html to PDF |
| interface [IPdfPageConfig](./HtmlPdfPlus/IPdfPageConfig.md) | Fluent interface commands to configure PDF rendering. |
| class [PageMargins](./HtmlPdfPlus/PageMargins.md) | Page margins. |
| enum [PageOrientation](./HtmlPdfPlus/PageOrientation.md) | Orientation Page PDF |
| class [PageSize](./HtmlPdfPlus/PageSize.md) | Page size for PDF. |
| class [PdfPageConfig](./HtmlPdfPlus/PdfPageConfig.md) | The Config PDF page. |
| enum [RenderMode](./HtmlPdfPlus/RenderMode.md) | Explicit declaration of how [`Html`](./HtmlPdfPlus/RequestHtmlPdf-1/Html.md) must be interpreted by the server, replacing the previous heuristic (`Uri.IsWellFormedUriString`) that inferred it from the string's shape. |
| class [RequestHtmlPdf&lt;T&gt;](./HtmlPdfPlus/RequestHtmlPdf-1.md) | Request data to convert Html to PDF |

### See Also
* [Main Index](../docindex.md)
