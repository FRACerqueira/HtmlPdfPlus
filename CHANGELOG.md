# Changelog

All notable changes to HtmlPdfPlus are documented here. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); this project
uses [Semantic Versioning](https://semver.org/).

## [2.0.0]

A major version because two changes break wire compatibility with 1.x
clients talking to a 2.x server (see below) — anything sending or parsing
the HTTP contract directly needs to be updated together with the packages.

### Added
- `RenderMode` and an explicit URL allow-policy, replacing implicit HTML/URL
  sniffing and closing an SSRF path where a server could be made to fetch an
  attacker-controlled URL.
- `MapHtmlPdfEndpoints()` and `MapHtmlPdfHealthEndpoints()` minimal-API
  extensions, plus `AddOpenApi()` support, so the request/response and
  health contracts are described by the library instead of hand-wired per
  host.
- Backpressure signaling: a saturated page pool now returns
  `ErrorCode.PoolExhausted` with `ErrorInfo.RetryAfterSeconds` and a real
  `Retry-After` HTTP header, instead of an unqualified failure or blocking
  until a page frees up.
- End-to-end deadline propagation (`SentAtUtc`): the client's timeout budget
  travels with the request so the server can stop working on a call the
  client has already given up on.
- Liveness/readiness endpoints (`/healthz`, `/readyz`) exposing
  `HtmlPdfHealthStatus` (browser connectivity, recovery state, available
  pages).
- Metrics via `System.Diagnostics.Metrics` under the `HtmlPdfPlus` meter:
  pool depth/acquire-wait, request duration, error counts by `ErrorCode`,
  and browser-restart counts — observable with any listener, no bundled
  exporter.
- Automatic browser recovery: if the underlying Chromium process dies, the
  page pool detects it and respawns the browser instead of failing every
  subsequent request, retrying the relaunch itself (3 attempts, 1s/2s
  backoff, ~93s worst case) instead of giving up after a single failure.
- `HtmlPdfHealthStatus.PoolStarved`: a recovery that reconnects the browser
  but cannot create a single usable page is now reported as unhealthy
  (`/readyz` 503) instead of falsely reporting ready.
- `ErrorCode.RenderFailed` is now actually produced (502, retryable) for a
  browser/page failure mid-render, instead of falling through to a
  non-retryable `Internal`/500 - the exact case automatic recovery exists to
  make succeed on retry.
- `Logger()` (client and server) now accepts `Warning`/`Error`/`Critical`,
  and a fully-exhausted recovery failure is logged at `Error` so it survives
  a host's default minimum log level - both used to be structurally capped
  at `Information`/`Debug`, so a failure like this could be invisible by
  default.
- A `HostApplicationLifetime.ApplicationStopped` registration now disposes
  the server's browser/pool as a backstop, in addition to normal DI-container
  disposal, so hosting patterns that call `StopAsync()` without ever
  disposing the container don't leak the Chromium process.
- Java client sample (`samples/JavaClientSendHttp`) demonstrating the wire
  format from outside .NET with no build tool or dependency.
- Samples for the new resilience features: `RetryAfterBackpressure` and
  `MetricsObserver`.

### Changed
- **Breaking:** `Canceled` is now HTTP-mapped to 503 (was 400), matching its
  `Retryable: true` semantics and staying distinct from `Timeout`'s 504.
- **Breaking:** the HTTP response for a `byte[]` output is now the raw PDF
  body (`application/pdf`), not a JSON envelope with a base64
  string. Non-2xx responses now carry the structured `ErrorInfo` contract
  in the body instead of a bare error message.
- **Breaking:** the HTTP request body is now sent as raw (optionally
  gzipped) bytes, not a base64/JSON-string-wrapped payload.
- `HtmlPdfResult` failures now carry a structured `ErrorInfo`/`ErrorCode`
  instead of a raw `Exception`, so callers can branch on failure kind
  without parsing exception messages or types.
- The page pool now waits for a page to free up natively (async) instead of
  polling, and pages are no longer closed while a render may still be
  running on them.
- `.Timeout()` is now enforced locally on the HTTP submit path, independent
  of the receiving server's own timeout handling.
- Samples reorganized: deduplicated shared HTML fixtures, fixed several
  defects, and renamed the `OnlyAtServer` projects for clarity.
- Docker image rebuilt: fixed a broken Chrome download URL that had made
  the image fail to build since 2025-11-13, and cut the working image size
  from 763MB to 370MB by keeping only the `chromium_headless_shell`
  variant Playwright actually launches (see [docs/guide/docker.md](docs/guide/docker.md)).
- Project version pinned to `2.0.0` via `<Version>` in the Client, Server
  and Shared `.csproj` files.

### Fixed
- A timeout race that could leave `HtmlPdfResult` `null` or falsely
  reported as successful.
- `IHtmlPdfClient.Run(HttpClient, string endpoint, ...)`'s `endpoint`
  parameter is now nullable, matching the implementation (and the 2-arg
  overloads that already relied on passing `null` internally).

### Removed
- **Breaking:** `HtmlPdfResult<T>.DecompressOutputData()`. ADR-003 already
  made the library's own `byte[]` output raw/uncompressed, so this method
  had no correct use against real output - calling it on an actual PDF threw
  a misleading "not a valid GZip stream" error instead of a useful one.

### Security
- Decompressed request size is now capped, closing a zip-bomb vector in
  request decompression.
- See **Added** above: the `RenderMode`/URL-allow-policy change is
  primarily a security fix (SSRF), not a feature addition.

## [1.0.1]
- Updated Playwright to version 1.56.0.
- Adjusted package reference for target framework (removed NetStandard2.1).
- Updated documentation.
- Added target .NET 10.0.

## [1.0.0]
- Updated Playwright to version 1.51.0.
- Adjusted package reference for target framework.
- Updated documentation.
- General availability (jump to version 1.0.0).

## [0.5.0-rc]
- Simplified sending data to the server via the HTTP client (now accepts
  `byte[]` instead of a stream).
- Removed the `ReadToBytesAsync` stream extension method.
- Exposed the `RequestHtmlPdf<T>` class for scenarios that need to handle
  sending parameters directly.
- Updated documentation.
- Preparation for the general-availability version.

## [0.4.0-rc]
- Relaxed package reference from .NET 8-only to .NET 8/.NET 9.
- Renamed the `Source` command to `Scope`.
- Renamed the `Request` command to `ScopeRequest`.
- Changed the `SubmitHtmlToPdf` function parameter to `byte[]` instead of
  `string`.
- Changed the `Run` and `ScopeRequest` command parameters to `byte[]`
  instead of `string`.
- Removed the `DecompressBytes()` method from `HtmlPdfResult`.
- Added `DecompressOutputData()` to `HtmlPdfResult` for custom scenarios.
- Made the compression/decompression process asynchronous.

## [0.3.0-beta]
- Added the `FromUrl(Uri value)` command to client-side mode.
- Fixed a thread-safety bug in server mode when parameter customization
  and/or non-client-mode sending was involved:
  - Moved `BeforePDF(Func<string, TIn?, CancellationToken, Task<string>>)`
    into the execution context.
  - Moved `AfterPDF(Func<byte[]?, TIn?, CancellationToken, Task<TOut>>)`
    into the execution context.
  - Added `Source(TIn? inputparam = default)` to carry an input parameter
    into the server execution context for custom actions and the HTML
    source.
  - Added `Request(string requestClient)` to carry client request data into
    the server execution context for custom actions and the HTML source.
  - Simplified server-side execution commands around a fluent execution
    context:
    - Removed the static `RequestHtmlPdf` class.
    - Added `FromHtml(string html, int converttimeout = 30000, bool minify = true)`.
    - Added `FromUrl(Uri value, int converttimeout = 30000)`.
    - Added `FromRazor<T>(string template, T model, int converttimeout = 30000, bool minify = true)`.

## [0.2.0-beta]
- Initial version.

[2.0.0]: https://github.com/FRACerqueira/HtmlPdfPlus/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/FRACerqueira/HtmlPdfPlus/releases/tag/v1.0.1
[1.0.0]: https://github.com/FRACerqueira/HtmlPdfPlus/releases/tag/v1.0.0
