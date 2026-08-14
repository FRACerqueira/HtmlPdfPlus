![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### Architecture

This page explains how HtmlPdfPlus is put together internally: what each package owns, the one distinction that determines whether a request gets compressed, how the page pool and browser lifecycle work, and where configuration is expected to live. It assumes you've already seen the three transport flows in the main [README](../../README.md#usage) - this is the "how" behind those diagrams, not another restatement of them.

#### Three packages, one namespace

| Package | Owns |
| --- | --- |
| `HtmlPdfPlus.Shared` | The wire-level data contracts: `RequestHtmlPdf<T>`, `HtmlPdfResult<T>`, `ErrorInfo`/`ErrorCode`, `RenderMode`, `DisableOptionsHtmlToPdf`. Referenced by both Client and Server so they always agree on the shape of a request/response. |
| `HtmlPdfPlus.Client` | `HtmlPdfClient` - the fluent builder used by a process that does **not** run the browser itself: it builds a `RequestHtmlPdf<T>`, optionally gzips it, and hands the bytes to whatever transport you give it (`HttpClient`, TCP, a queue). |
| `HtmlPdfPlus.Server` | `HtmlPdfBuilder`/`HtmlPdfServer` - the Playwright-backed renderer: the page pool, the browser lifecycle, and the two entry paths described below. Also the ASP.NET Core endpoint extensions (`MapHtmlPdfEndpoints`, `MapHtmlPdfHealthEndpoints`), which are a thin convenience layer over the same server, not a separate implementation. |

All three share the `HtmlPdfPlus` namespace (endpoint extensions live in `Microsoft.AspNetCore.Routing` instead, so they show up via `using Microsoft.AspNetCore.Routing;` the same way other ASP.NET Core mapping extensions do). `Shared` declares `InternalsVisibleTo` for both `Client` and `Server` (and the test project), so its internal helpers (e.g. `GZipHelper`) are usable directly by either package - there is no public API surface between `Client` and `Server` beyond what `Shared` exposes.

#### The distinction that matters most: `ScopeData()` vs `ScopeRequest(bytes)`

`IHtmlPdfServer<TIn, TOut>` exposes two ways to start a conversion, and they are not interchangeable:

- **`ScopeData()`** - the in-process path. You call `FromHtml`/`FromRazor`/`FromUrl` directly on the context; there is no serialized payload anywhere, because there is no process boundary to cross. **No compression or decompression ever happens on this path** - `DisableOptionsHtmlToPdf.DisableCompress` has no effect here, because there is nothing to disable.
- **`ScopeRequest(byte[] requestClient)`** (and the equivalent `Run(byte[])` overload) - the received-payload path. A `RequestHtmlPdf<T>` arrived from somewhere outside this process - an HTTP body, a TCP frame, a queue message - and this path deserializes it, decompressing first unless `DisableCompress` is set on the builder.

`MapHtmlPdfEndpoints()` is built entirely on `ScopeRequest`: it reads the raw request body into a byte array and calls `ScopeRequest(bytes).Run(...)`, exactly like a hand-written TCP listener would (see [ClientSendTcp](../../samples/ConsoleHtmlToPdfPlus.ClientSendTcp) / [TcpServerHtmlToPdf.GenericServer](../../samples/TcpServerHtmlToPdf.GenericServer)). This is what "transport-agnostic by design" means in practice - the library never owns a socket; every transport, including the built-in HTTP one, is a thin adapter around `ScopeRequest`. See [ADR-003](../adr/ADR003V01R01-serve-and-accept-raw-bytes-instead-of-base64-json-wrapping.md) for why that payload is raw bytes rather than base64/JSON.

Getting this backwards is an easy mistake to make from reading configuration alone: setting `DisableFeatures(DisableOptionsHtmlToPdf.DisableCompress)` on a builder that is only ever used via `ScopeData()` compiles fine and does nothing observable, because the flag is only consulted inside the `ScopeRequest`/`Run(byte[])` code path.

#### Page pool and browser lifecycle

Each `HtmlPdfBuilder` owns one Playwright browser instance and a pool of pre-opened pages:

- `PagesBuffer` (default `5`) sets how many `IPage` instances are kept warm in a `ConcurrentQueue<IPage>`, gated by a `SemaphoreSlim` that a request waits on to acquire one.
- `AcquireTimeout` (default `5000`ms) bounds that wait. If no page frees up in time, the request fails with `ErrorCode.PoolExhausted` and a suggested `RetryAfterSeconds` - see [ADR-004](../adr/ADR004V01R01-signal-backpressure-instead-of-retrying-internally.md) and the [resilience guide](resilience.md) for the full flow.
- If the underlying Chromium process disconnects unexpectedly, the `Disconnected` event drives `RecoverBrowserAsync()`, which discards the dead pool, relaunches the browser, and refills it. Concurrent disconnect notifications (multiple pages can fault around the same crash) are collapsed into a single attempt via `Interlocked.CompareExchange` - the recovery runs once, not once per faulted page.
- Metrics are published per builder instance through its own `Meter` (not the process-wide static one in `HtmlPdfMetrics`), so disposing one `AddHtmlPdfService` registration unregisters only its own observable gauge, without silencing the counters/histograms every other registration reports through.

#### Extension points: `BeforePDF` / `AfterPDF`

Both hooks live on the **execution context** (the object returned by `ScopeData()`/`ScopeRequest()`), not on the builder. This was a deliberate fix, not the original design: an earlier version attached them to the builder itself, which was not thread-safe when multiple requests customized the hooks concurrently (see the 0.3.0-beta entry in [CHANGELOG.md](../../CHANGELOG.md)). Attaching them per-context means each request's customization is isolated from every other concurrent request on the same builder.

#### Where configuration lives

| Level | Examples | Set via |
| --- | --- | --- |
| Builder (shared by every request on this instance) | `PagesBuffer`, `AcquireTimeout`, `DisableFeatures`, `UrlAllowPolicy`, `MaxDecompressedRequestSize`, `DefaultConfig` | `AddHtmlPdfService((cfg) => cfg. ...)` |
| Per call (this request only) | `FromHtml`/`FromUrl` timeout, `BeforePDF`/`AfterPDF` | Fluent calls on the execution context |

`PageConfig` is the one exception worth calling out: it is set by the **client** (`HtmlPdfClient.PageConfig(...)`) when building a `RequestHtmlPdf<T>` that will cross a wire, and travels with the request. On the server, `ScopeRequest`/`Run(byte[])` falls back to the builder's `DefaultConfig` only if the incoming request didn't carry one (`requestHtmlPdf.Config ??= builder.Config`) - it does not merge the two. The pure `ScopeData()` path never involves a client at all, so it always uses the builder's `DefaultConfig` directly.

### See Also
* [Main README](../../README.md)
* [Guide index](README.md)
* [How-To index](howto/README.md)
* [Resilience and observability](resilience.md)
* [API Reference](../api/docindex.md)
* [Architecture Decision Records](../adr/indexadrs.md)
