![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### Resilience and observability

The three flows in the main [README](../../README.md#usage) show who calls whom for each transport. This page covers the flows that cut across all three transports: what happens when the page pool is saturated, when the underlying browser process dies, and how a host observes renderer health from outside. None of these are new transports - they are the same `Run()` call reacting to a specific condition.

#### Backpressure and Retry-After

The page pool has a finite number of pages (`PagesBuffer`). When every page is busy and none frees up within `AcquireTimeout`, the request fails with `ErrorCode.PoolExhausted` instead of blocking indefinitely or failing silently. The library never retries this internally - it signals the condition and leaves the retry decision entirely to the caller (see [ADR-004](../adr/ADR004V01R01-signal-backpressure-instead-of-retrying-internally.md)).

```mermaid
sequenceDiagram
    participant Caller
    participant HtmlPdfServer
    participant Pool as Page pool

    Caller->>HtmlPdfServer: Run
    HtmlPdfServer->>Pool: Acquire page (wait up to AcquireTimeout)
    Pool-->>HtmlPdfServer: No page freed up in time

    HtmlPdfServer-->>Caller: HtmlPdfResult (ErrorCode.PoolExhausted, RetryAfterSeconds)
    Note over Caller: Over HTTP, MapHtmlPdfEndpoints reflects this as 503 + a real Retry-After header
    Caller-->>Caller: Wait RetryAfterSeconds, then retry (caller's own policy)
    Caller->>HtmlPdfServer: Run (retry)
```

See the [RetryAfterBackpressure](../../samples/ConsoleHtmlToPdfPlus.RetryAfterBackpressure) sample for a working caller-owned retry loop.

#### Automatic browser recovery

If the underlying Chromium process dies unexpectedly (a crash, being killed by the OS under memory pressure, etc.), the page pool detects the disconnect and relaunches the browser instead of failing every request that follows. Concurrent disconnect notifications from multiple pages collapse into a single recovery attempt.

```mermaid
sequenceDiagram
    participant Chromium as Chromium process
    participant Builder as HtmlPdfBuilder
    participant Pool as Page pool

    Chromium--xBuilder: Disconnected event (unexpected crash)
    Builder-->>Builder: Discard queued pages from the dead browser
    Builder->>Chromium: Relaunch Chromium
    Chromium-->>Builder: New browser instance connected
    Builder->>Pool: Refill the page pool
    Note over Builder: htmlpdfplus.browser.restarts incremented (see the metrics guide below)
```

Requests that arrive mid-recovery see `HtmlPdfHealthStatus.Recovering = true` via the readiness endpoint below, rather than an opaque failure.

#### Health and readiness

`MapHtmlPdfHealthEndpoints()` maps two endpoints with deliberately different meanings: liveness answers "is the process itself responsive", readiness answers "can this instance actually render right now".

```mermaid
sequenceDiagram
    participant Orchestrator
    participant HealthEndpoints as Health endpoints
    participant Server as HtmlPdfServer

    Orchestrator->>HealthEndpoints: GET /healthz
    HealthEndpoints-->>Orchestrator: 200 OK (the process resolved the service without throwing)

    Orchestrator->>HealthEndpoints: GET /readyz
    HealthEndpoints->>Server: GetHealthStatus()
    Server-->>HealthEndpoints: HtmlPdfHealthStatus(BrowserConnected, Recovering, AvailablePages)
    alt Browser connected and not recovering
        HealthEndpoints-->>Orchestrator: 200 OK + HtmlPdfHealthStatus
    else Browser disconnected or mid-recovery
        HealthEndpoints-->>Orchestrator: 503 Service Unavailable + HtmlPdfHealthStatus
    end
```

Liveness deliberately never inspects the browser/pool: a crashed browser is a readiness concern (pull this instance out of rotation while it self-heals), not a liveness one (restarting the whole process would only discard an in-progress auto-recovery). A momentarily empty pool is likewise still "ready" - that is what the backpressure signal above is for, not a readiness failure.

#### Metrics

Every instance publishes instruments under the `HtmlPdfPlus` meter via `System.Diagnostics.Metrics` - no bundled exporter, so any OpenTelemetry-compatible backend can consume them with a single `AddMeter("HtmlPdfPlus")` call (see [ADR-005](../adr/ADR005V01R01-metrics-via-system-diagnostics-metrics-with-no-bundled-exporter.md)):

| Instrument | Kind | Tags | What it tells you |
| --- | --- | --- | --- |
| `htmlpdfplus.pool.available_pages` | Observable gauge | `sourcealias` | Current pool depth |
| `htmlpdfplus.pool.acquire_wait` | Histogram (ms) | `sourcealias`, `outcome` (`acquired`/`pool_exhausted`/`canceled`) | Time spent waiting for a page |
| `htmlpdfplus.request.duration` | Histogram (ms) | `sourcealias`, `success` | Render + hook time for requests that reached a render attempt |
| `htmlpdfplus.errors` | Counter | `sourcealias`, `error_code` | Failures by `ErrorCode`, including request-level validation failures |
| `htmlpdfplus.browser.restarts` | Counter | `sourcealias` | Automatic browser relaunches after an unexpected disconnect |

See the [MetricsObserver](../../samples/ConsoleHtmlToPdfPlus.MetricsObserver) sample for a working `MeterListener` that needs no exporter package at all.

### See Also
* [Main README](../../README.md)
* [Architecture Decision Records](../adr/indexadrs.md)
