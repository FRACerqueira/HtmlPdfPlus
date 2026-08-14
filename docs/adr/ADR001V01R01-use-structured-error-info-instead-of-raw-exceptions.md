<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values Migrated |
|--|--|
|File title md|Use structured ErrorInfo instead of raw exceptions|
|Version|01|
|Revision|01|
|Scope||
|Domain||
|Created|Proposed (2026-08-14)|
|Changed|Accepted (2026-08-14)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Use structured ErrorInfo instead of raw exceptions

## Deciders

* Deciders: Fernando Cerqueira

Technical Story: commit `8a92e59` - replace raw Exception with structured ErrorInfo in HtmlPdfResult

## Context and Problem Statement

Before v2, a failed `HtmlPdfResult<T>` carried the raw `Exception` that caused it. A thrown `Exception` can fail to serialize with `System.Text.Json` (its `TargetSite` is not supported), and even when serialization succeeds, `Exception.Message` has no public setter, so the original message does not survive a round-trip over HTTP. Callers also had no stable, language-agnostic value to branch on: matching .NET exception types works for an in-process caller, but a client written in Java, Node, or curl has no equivalent to catch `TimeoutException`. How should a failure be represented so any caller, in any language, can act on it reliably?

## Decision Drivers

* The failure must survive a JSON round-trip over HTTP without losing information.
* A non-.NET client must be able to classify the failure without parsing free-text messages or .NET type names.
* Backpressure-style failures need to carry a machine-actionable retry hint (see [ADR-004](./ADR004V01R01-signal-backpressure-instead-of-retrying-internally.md)).

## Considered Options

* Keep returning the raw `Exception`
* Return only an HTTP status code, no structured body
* Introduce a structured `ErrorInfo` DTO with a stable `ErrorCode` enum

## Decision Outcome

Chosen option: "Introduce a structured `ErrorInfo` DTO with a stable `ErrorCode` enum", because it is the only option that both survives serialization intact and gives every caller - regardless of language - a stable value to branch on. `ErrorInfo` carries an `ErrorCode`, a human-readable `Message`, a `Retryable` flag, and an optional `RetryAfterSeconds`. `ErrorInfo.FromException(Exception)` classifies well-known .NET exception types (`TimeoutException`, `OperationCanceledException`, `ArgumentException`/`InvalidOperationException`) into the corresponding `ErrorCode` once, at the point of capture, instead of once per caller language.

### Positive Consequences

* Any client, regardless of language, can branch on `ErrorCode` instead of parsing exception type names or messages.
* The wire contract (`ErrorInfo` in the HTTP body) survives serialization round-trips completely, including on non-2xx responses.
* `RetryAfterSeconds` gives backpressure signals (`ErrorCode.PoolExhausted`) a machine-actionable delay, not just a bare failure.

### Negative Consequences

* The classification in `ErrorInfo.FromException` is necessarily lossy: a caught exception's stack trace and full .NET type are not part of `ErrorInfo` and cannot be recovered from it. Diagnosing an `ErrorCode.Internal` failure in depth still requires server-side logs, not just the client-visible `ErrorInfo`.

## Pros and Cons of the Options

### Keep returning the raw `Exception`

* Good, because no code change was required.
* Bad, because it does not survive JSON serialization intact (`TargetSite` unsupported, `Message` has no setter on deserialize).
* Bad, because non-.NET clients have nothing to branch on.

### Return only an HTTP status code, no structured body

* Good, because it is the simplest possible contract.
* Bad, because a single status code (e.g. 500) cannot distinguish `PoolExhausted` from `RenderFailed` from `Internal`, all of which need different client behavior.

### Introduce a structured `ErrorInfo` DTO with a stable `ErrorCode` enum

* Good, because it survives serialization and is language-agnostic.
* Good, because it can carry a retry hint (`RetryAfterSeconds`) alongside the classification.
* Bad, because it is a new public contract that must be kept stable across versions.

## Links

* Related to [ADR-004](./ADR004V01R01-signal-backpressure-instead-of-retrying-internally.md) - `ErrorInfo.RetryAfterSeconds` is what makes the backpressure signal actionable.
