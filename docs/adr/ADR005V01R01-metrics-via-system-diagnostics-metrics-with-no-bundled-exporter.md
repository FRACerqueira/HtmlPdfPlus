<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values Migrated |
|--|--|
|File title md|Metrics via System Diagnostics Metrics with no bundled exporter|
|Version|01|
|Revision|01|
|Scope||
|Domain||
|Created|Proposed (2026-08-14)|
|Changed|Accepted (2026-08-14)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Metrics via System.Diagnostics.Metrics with no bundled exporter

## Deciders

* Deciders: Fernando Cerqueira

Technical Story: commits `c57be58` (pool depth/duration/restarts) and `92696cb` (error-by-code, acquire-wait)

## Context and Problem Statement

Before v2, the library exposed no runtime signal about pool health, render duration, or failure rates - an operator had no way to see pool contention or browser instability building before it surfaced as a customer-visible failure. Any metrics story needs to pick both an instrumentation API and a decision about whether the library ships a specific backend integration (Prometheus, OTLP, Application Insights, ...) or leaves that choice to the host.

## Decision Drivers

* Hosts already standardize on very different metrics backends; the library should not force one.
* .NET has a built-in, vendor-neutral instrumentation API (`System.Diagnostics.Metrics`) that any OpenTelemetry-compatible exporter already knows how to consume.
* Adding a specific exporter package as a dependency would pull in transitive dependencies every host pays for, whether or not it uses that particular backend.

## Considered Options

* Bundle a specific exporter (e.g. an OpenTelemetry+Prometheus package) as a dependency
* Log metrics as structured log lines instead of using a metrics API
* Instrument with `System.Diagnostics.Metrics` under a named `Meter`, ship no exporter

## Decision Outcome

Chosen option: "Instrument with `System.Diagnostics.Metrics` under a named `Meter` (`HtmlPdfPlus`), ship no exporter", because it is the only option that gives every host a real metrics API without dictating a backend. A host wanting Prometheus/OTLP/Application Insights adds its own OpenTelemetry exporter and calls `AddMeter("HtmlPdfPlus")` - no HtmlPdfPlus-specific integration code needed. The `MetricsObserver` sample demonstrates that even a host with no exporter package at all can observe every instrument with a bare `MeterListener` from the .NET runtime itself.

### Positive Consequences

* Zero additional package dependency for metrics - `System.Diagnostics.Metrics` ships in the .NET runtime.
* Works with any OpenTelemetry-compatible backend a host already uses, with a single `AddMeter` call.
* `htmlpdfplus.pool.acquire_wait`, `.request.duration`, `.errors` (tagged by `ErrorCode`) and `.browser.restarts` together let an operator distinguish pool contention from slow renders from outright browser failures.

### Negative Consequences

* A host that wants metrics gets nothing until it wires up its own exporter and listener - there is no zero-configuration dashboard out of the box.
* `.request.duration` deliberately excludes requests that failed request-level validation before a render was attempted (tracked instead in `.errors`), so the two counters do not reconcile under a flood of malformed requests - this is intentional, but easy to misread without reading the metric descriptions.

## Pros and Cons of the Options

### Bundle a specific exporter as a dependency

* Good, because a host gets working metrics with zero setup, for that one backend.
* Bad, because every host pays the dependency cost even if it uses a different backend, or none.
* Bad, because it locks the library's release cadence to that exporter's own compatibility surface.

### Log metrics as structured log lines

* Good, because every host already has logging wired up.
* Bad, because logs are not built for aggregation (histograms, rates) the way a metrics API is - an operator would need to reconstruct that in their log pipeline.

### Instrument with `System.Diagnostics.Metrics`, ship no exporter

* Good, because it is vendor-neutral and dependency-free.
* Good, because any OpenTelemetry-based backend already knows how to consume it.
* Bad, because a host must do its own wiring to see anything - there is no default output.

## Links

* Related to [ADR-004](./ADR004V01R01-signal-backpressure-instead-of-retrying-internally.md) - `htmlpdfplus.pool.acquire_wait` surfaces the same pool contention that `ErrorCode.PoolExhausted` signals per-request.
