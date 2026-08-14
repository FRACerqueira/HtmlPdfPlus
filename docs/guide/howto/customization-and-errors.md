![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### How-To: Customizing the pipeline and handling failures

Use case: you need to change what goes in or comes out of a conversion, or react correctly when one fails. For production-scale failure handling (retry, health, metrics), see the [resilience guide](../resilience.md) - this page covers the per-call mechanics.

#### Substitute tokens or normalize HTML before rendering

```csharp
.BeforePDF((html, inputParam, token) => Task.FromResult(html.Replace("[{Token}]", inputParam.Value)));
```
`BeforePDF`/`AfterPDF` live on the execution context, not the builder, so concurrent requests never share customization state - see [Extension points](../architecture.md#extension-points-beforepdf--afterpdf) in the architecture guide. Sample: [OnlyAtServer/CustomHooks](../../../samples/ConsoleHtmlToPdfPlus.OnlyAtServer/CustomHooks).

#### Return something other than raw PDF bytes (e.g. a saved file path)

```csharp
services.AddHtmlPdfService<DataSavePDF, string>((cfg) => cfg. ...);
// ...
.AfterPDF((pdfBytes, inputParam, token) => { /* save it, return the path */ });
```
Samples: [ClientCustomSendHttp](../../../samples/ConsoleHtmlToPdfPlus.ClientCustomSendHttp) / [WebHtmlToPdf.CustomSaveFileServer](../../../samples/WebHtmlToPdf.CustomSaveFileServer).

#### Handle failures by category, not by exception type

```csharp
if (!pdfresult.IsSuccess)
{
    switch (pdfresult.Error!.Code)
    {
        case ErrorCode.PoolExhausted: /* see the retry how-to below */ break;
        case ErrorCode.Timeout or ErrorCode.Canceled: /* retryable */ break;
        case ErrorCode.InvalidRequest: /* fix the request, don't retry */ break;
        default: /* log pdfresult.Error.Message */ break;
    }
}
```
See [ADR-001](../../adr/ADR001V01R01-use-structured-error-info-instead-of-raw-exceptions.md) for why failures are a structured `ErrorInfo`, not a raw exception.

#### Retry when the pool is temporarily saturated

`ErrorCode.PoolExhausted` comes with a suggested `ErrorInfo.RetryAfterSeconds` - the library never retries on your behalf, so your own loop decides. Full flow and diagram: [resilience guide - backpressure](../resilience.md#backpressure-and-retry-after). Sample: [RetryAfterBackpressure](../../../samples/ConsoleHtmlToPdfPlus.RetryAfterBackpressure).

### See Also
* [How-To index](README.md)
* [Architecture guide](../architecture.md)
* [Resilience and observability](../resilience.md)
