<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values Migrated |
|--|--|
|File title md|Explicit RenderMode with a URL allow-policy to close SSRF|
|Version|01|
|Revision|01|
|Scope||
|Domain||
|Created|Proposed (2026-08-14)|
|Changed|Accepted (2026-08-14)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Explicit RenderMode with a URL allow-policy to close SSRF

## Deciders

* Deciders: Fernando Cerqueira

Technical Story: commit `cf77a41` - replace HTML/URL sniffing with explicit RenderMode and an SSRF-safe URL policy

## Context and Problem Statement

Before v2, the server inferred whether `RequestHtmlPdf<T>.Html` was literal markup or a URL to navigate to by heuristically testing the string's shape (`Uri.IsWellFormedUriString`). Any string that happened to look like a URL was navigated to, including one supplied by an untrusted caller pointing at an internal address (e.g. a cloud metadata endpoint like `169.254.169.254`) - a server-side request forgery (SSRF) vector. How should the server decide when to navigate versus render literal HTML, and how should it stop that navigation from reaching addresses it should never reach?

## Decision Drivers

* Sniffing a string's shape is not a security boundary - a caller can shape a payload to be misclassified.
* The server must not navigate to internal/private/link-local addresses on the caller's behalf.
* The policy must be overridable per host, since what counts as "safe to reach" depends on the deployment's own network topology.

## Considered Options

* Keep shape-based sniffing, add a denylist of known-bad hosts
* Add an explicit `RenderMode` (`Html`/`Url`) plus a default-deny URL allow-policy, overridable via `UrlAllowPolicy(Func<Uri, bool>)`
* Require callers to pre-resolve URLs to content themselves, drop server-side URL fetching entirely

## Decision Outcome

Chosen option: "Add an explicit `RenderMode` (`Html`/`Url`) plus a default-deny URL allow-policy, overridable via `UrlAllowPolicy(Func<Uri, bool>)`", because it removes the ambiguity at the source (the caller states its intent) and gives every host a closed-by-default policy it can widen deliberately, instead of a denylist that must be kept exhaustive by hand. The default policy (`HtmlPdfBuilder.DefaultUrlPolicy`) allows only `http`/`https` and denies loopback, private and link-local IP literals - covering the most common SSRF target class, including cloud metadata endpoints.

### Positive Consequences

* Intent is explicit: a caller declares `RenderMode.Url` instead of the server guessing from string shape.
* The default policy closes the most common SSRF vector with no host configuration required.
* Hosts with unusual network topology can supply their own `Func<Uri, bool>` via `UrlAllowPolicy`.

### Negative Consequences

* The default policy only inspects the URL when its host is already a literal IP address. A DNS hostname that resolves to a private or link-local address at connect time (Chromium does its own resolution) is not caught - a host concerned about DNS-rebinding-style attacks must supply a DNS-aware policy via `UrlAllowPolicy`.

## Pros and Cons of the Options

### Keep shape-based sniffing, add a denylist of known-bad hosts

* Good, because it requires no change to the request contract.
* Bad, because a denylist is inherently incomplete and must be maintained as new bad targets are discovered.
* Bad, because sniffing still leaves the door open for a specially-shaped literal-HTML payload to be misclassified as a URL, or vice versa.

### Add an explicit `RenderMode` plus a default-deny URL allow-policy

* Good, because intent is explicit and the default policy is closed, not open.
* Good, because the policy is overridable per host without a library change.
* Bad, because it is a breaking change to the request contract (adds a required field callers must set correctly).

### Require callers to pre-resolve URLs to content themselves

* Good, because it removes SSRF risk from the server entirely.
* Bad, because it removes a core feature (`FromUrl`) that the library exists to provide.

## Links

* Related to [ADR-003](./ADR003V01R01-serve-and-accept-raw-bytes-instead-of-base64-json-wrapping.md) - `RenderMode` was introduced in the same request-contract generation as the raw-bytes wire format change.
