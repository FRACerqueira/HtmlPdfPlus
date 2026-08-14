<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values Migrated |
|--|--|
|File title md|Signal backpressure instead of retrying internally|
|Version|01|
|Revision|01|
|Scope||
|Domain||
|Created|Proposed (2026-08-14)|
|Changed|Accepted (2026-08-14)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Signal backpressure instead of retrying internally

## Deciders

* Deciders: Fernando Cerqueira

Technical Story: commit `1c4c09b` - signal backpressure with a real Retry-After header

## Context and Problem Statement

The page pool has a finite number of pages (`PagesBuffer`). Before v2, a request that could not acquire a page within `AcquireTimeout` simply failed - the caller had no way to distinguish "the pool is temporarily saturated, try again shortly" from any other failure, and no signal for how long "shortly" might be. Should the library retry internally on the caller's behalf, or surface the saturation as an actionable signal?

## Decision Drivers

* A caller (HTTP client, load balancer, orchestrator) is in a much better position than the library to decide whether/how to retry - it knows its own deadline budget and concurrency.
* Retrying internally would hide the real latency and could turn a transient local saturation into a cascading pile-up if many callers' internal retries overlap.
* HTTP already has a standard vocabulary for this (`503` + `Retry-After`) that any HTTP client, proxy, or orchestrator already understands.

## Considered Options

* Retry internally inside the library until a page is available or a hard timeout elapses
* Fail with a generic error, no distinction from other failure kinds
* Classify as `ErrorCode.PoolExhausted`, attach a suggested `RetryAfterSeconds`, and let the caller decide

## Decision Outcome

Chosen option: "Classify as `ErrorCode.PoolExhausted`, attach a suggested `RetryAfterSeconds`, and let the caller decide", because it keeps the retry policy - a caller-specific concern - out of the library, while still giving the caller everything needed to implement one. Over HTTP, `MapHtmlPdfEndpoints()` reflects `RetryAfterSeconds` as the standard `Retry-After` response header (see [ADR-001](./ADR001V01R01-use-structured-error-info-instead-of-raw-exceptions.md)), so even a caller that only reads HTTP semantics - not the JSON body - gets an actionable signal. The `RetryAfterBackpressure` sample demonstrates the caller-owned retry loop this decision expects.

### Positive Consequences

* No hidden retry storms inside the library; retry policy (backoff, max attempts, jitter) is entirely the caller's choice.
* Standard HTTP semantics (`503` + `Retry-After`) make the signal actionable even to callers that never inspect the JSON body - proxies and load balancers included.
* `htmlpdfplus.pool.acquire_wait` (see [ADR-005](./ADR005V01R01-metrics-via-system-diagnostics-metrics-with-no-bundled-exporter.md)) lets an operator see pool contention building before callers start seeing `PoolExhausted` at all.

### Negative Consequences

* Every caller that wants resilience to transient saturation must implement its own retry loop; the library provides the signal, not the behavior. A caller that ignores `ErrorCode.PoolExhausted` gets the same experience as any other failure.

## Pros and Cons of the Options

### Retry internally inside the library

* Good, because callers get resilience with no code of their own.
* Bad, because it hides real latency from the caller and can amplify saturation if many callers retry in lockstep.
* Bad, because the "right" retry policy is deployment-specific; baking one in serves no deployment well.

### Fail with a generic error, no distinction from other failure kinds

* Good, because it requires no new contract.
* Bad, because a caller cannot tell a transient saturation from a permanent failure, so it cannot make a sound retry decision at all.

### Classify as `PoolExhausted` with a `RetryAfterSeconds` hint

* Good, because it gives the caller everything needed to retry soundly, without dictating policy.
* Good, because it reuses standard HTTP semantics (`Retry-After`) that existing tooling already understands.
* Bad, because it still requires every caller to opt in to handling it - there is no free resilience.

## Links

* Related to [ADR-001](./ADR001V01R01-use-structured-error-info-instead-of-raw-exceptions.md) - `RetryAfterSeconds` is a field on `ErrorInfo`.
* Related to [ADR-005](./ADR005V01R01-metrics-via-system-diagnostics-metrics-with-no-bundled-exporter.md) - `htmlpdfplus.pool.acquire_wait` makes pool contention observable before it turns into `PoolExhausted`.
