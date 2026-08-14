![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### How-To: Client and server as separate processes

Use case: the client and the renderer live in different processes (or different machines), and you need to move a request/response between them. For the design behind these calls, see the [architecture guide](../architecture.md).

#### Run over HTTP

Server:
```csharp
builder.Services.AddOpenApi();
builder.Services.AddHtmlPdfService((cfg) => cfg.Logger(LogLevel.Debug, "MyPDFServer"));
// ...
app.MapOpenApi();
app.MapHtmlPdfEndpoints("/GeneratePdf");
```
Client:
```csharp
var pdfresult = await HtmlPdfClient.Create("MyClient").FromHtml(html).Run(httpClient, token);
```
`MapHtmlPdfEndpoints` also wires up the response contract that `AddOpenApi()`/`MapOpenApi()` describes, so the generated OpenAPI document is accurate. See the [Usage](../../../README.md#1-using-client-server-mode) section in the main README for the full flow and diagram. Samples: [WebHtmlToPdf.GenericServer](../../../samples/WebHtmlToPdf.GenericServer) (server), [ClientSendHttp](../../../samples/ConsoleHtmlToPdfPlus.ClientSendHttp) (client).

#### Use a transport other than HTTP (TCP, a queue, anything)

```csharp
var pdfresult = await HtmlPdfClient.Create("MyClient").FromHtml(html)
    .Run<TIn, TOut>(mySubmitFunc, inputParam, token);
```
`mySubmitFunc` is `Func<byte[], CancellationToken, Task<HtmlPdfResult<TOut>>>` - it only receives the request bytes and a cancellation token; `inputParam` (`TIn?`) is a separate argument to `Run`, not part of the delegate. You own the wire entirely; the library only builds the request bytes and parses the result. On the receiving end, call `ScopeRequest(bytes).Run(...)` with whatever bytes your transport delivered. Samples: [ClientSendTcp](../../../samples/ConsoleHtmlToPdfPlus.ClientSendTcp) / [TcpServerHtmlToPdf.GenericServer](../../../samples/TcpServerHtmlToPdf.GenericServer).

#### Call the server from a non-.NET client

The wire format is: build the JSON request, gzip it, POST it as `application/octet-stream`. The [JavaClientSendHttp](../../../samples/JavaClientSendHttp) sample is a single dependency-free `.java` file showing exactly that with the JDK's own `HttpClient` - use it as the reference for any other language. See [ADR-003](../../adr/ADR003V01R01-serve-and-accept-raw-bytes-instead-of-base64-json-wrapping.md) for why the wire format is raw bytes rather than base64/JSON.

### See Also
* [How-To index](README.md)
* [Architecture guide](../architecture.md)
* [Rendering content](rendering.md)
* [Customization and error handling](customization-and-errors.md)
