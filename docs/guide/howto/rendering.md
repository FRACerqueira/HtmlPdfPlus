![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### How-To: Rendering content

Use case: you have HTML, a URL, or a Razor template, and you need a PDF. For the design behind these calls, see the [architecture guide](../architecture.md).

#### Convert HTML in the same process (no client, no network)

```csharp
var pdfresult = await PDFserver.ScopeData().FromHtml(html).Run(cancellationToken);
```
No compression, no client involved - see [ScopeData vs ScopeRequest](../architecture.md#the-distinction-that-matters-most-scopedata-vs-scoperequestbytes) for why that matters. Sample: [OnlyAtServer/QuickStart](../../../samples/ConsoleHtmlToPdfPlus.OnlyAtServer/QuickStart).

#### Convert a URL safely

```csharp
var pdfresult = await HtmlPdfClient.Create("MyClient").FromUrl(new Uri(url)).Run(...);
```
By default the server only navigates to `http`/`https` URLs and refuses loopback, private and link-local IP literals - closing the most common SSRF path. This does **not** catch a DNS hostname that only resolves to a private address at connect time; if that matters for your deployment, supply your own policy:

```csharp
services.AddHtmlPdfService((cfg) => cfg.UrlAllowPolicy(uri => MyOwnRules(uri)));
```
See [ADR-002](../../adr/ADR002V01R01-explicit-render-mode-with-a-url-allow-policy-to-close-ssrf.md) for the full reasoning.

#### Render a Razor template with a typed model

```csharp
var pdfresult = await HtmlPdfClient.Create("MyClient").FromRazor(templateText, model).Run(...);
```
Sample: [ClientSendHttp](../../../samples/ConsoleHtmlToPdfPlus.ClientSendHttp).

#### Tune for throughput or reduce overhead

```csharp
services.AddHtmlPdfService((cfg) => cfg
    .PagesBuffer(10)          // more pre-warmed pages, more memory
    .AcquireTimeout(2000)     // fail fast into PoolExhausted instead of queuing long
    .DisableFeatures(DisableOptionsHtmlToPdf.DisableMinifyHtml | DisableOptionsHtmlToPdf.DisableLogging));
```
Raise `PagesBuffer` for more concurrent renders (at the cost of more Chromium memory); lower `AcquireTimeout` to surface backpressure sooner instead of making callers wait. `DisableCompress` only matters on the `ScopeRequest`/`Run(byte[])` path (see the [architecture guide](../architecture.md)) - setting it when every call goes through `ScopeData()` has no effect.

### See Also
* [How-To index](README.md)
* [Architecture guide](../architecture.md)
* [Transports](transports.md)
