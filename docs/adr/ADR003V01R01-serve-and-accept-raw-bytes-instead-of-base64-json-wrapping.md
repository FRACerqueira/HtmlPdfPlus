<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values Migrated |
|--|--|
|File title md|Serve and accept raw bytes instead of base64 JSON wrapping|
|Version|01|
|Revision|01|
|Scope||
|Domain||
|Created|Proposed (2026-08-14)|
|Changed|Accepted (2026-08-14)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Serve and accept raw bytes instead of base64/JSON wrapping

## Deciders

* Deciders: Fernando Cerqueira

Technical Story: commits `af4b01c` (response) and `b952c28` (request)

## Context and Problem Statement

Before v2, both directions of the HTTP wire format wrapped binary payloads in JSON: the request body was a JSON string carrying the (optionally gzip-compressed) payload, and a `byte[]` response was returned as a JSON envelope with the PDF bytes base64-encoded inside it. Base64 inflates payload size by roughly a third, and the JSON-string wrapping adds a decode step neither side actually needs, since the payload is already a byte sequence at both ends. Should the wire format keep wrapping binary data as JSON/base64, or send it as what it already is?

## Decision Drivers

* PDF payloads are often several hundred KB to a few MB; base64 overhead is not negligible at that size.
* Every non-.NET client (see the Java sample) has to implement whatever encoding is chosen - a plain byte stream is trivial in any language, base64-inside-JSON is one more library/step.
* The change is necessarily breaking for any 1.x client talking to a 2.x server, so it needed to happen once, deliberately, rather than incrementally.

## Decision Outcome

Chosen option: "Send raw bytes as `application/octet-stream`, keep JSON only for non-`byte[]` output types", because it removes the base64 overhead and the encode/decode step on both ends, and the resulting contract is simpler to implement correctly from any language - a client just posts bytes and reads bytes back. The request is bound server-side as a raw `Stream`, not `[FromBody] byte[]`, specifically so no base64/JSON-string wrapping is layered on top purely because of `byte[]` model-binding defaults. A successful `byte[]` output is served as the raw PDF body (`application/pdf`); any other output type, and any failure (via the structured `ErrorInfo` contract - see [ADR-001](./ADR001V01R01-use-structured-error-info-instead-of-raw-exceptions.md)), is still served as JSON, since those are not raw binary payloads.

### Positive Consequences

* No base64 overhead on the largest payloads (the PDF itself and the compressed request body).
* Any HTTP client in any language can produce/consume the contract with just its standard byte-stream APIs - demonstrated by the dependency-free Java sample.
* The OpenAPI document generated from `MapHtmlPdfEndpoints()` accurately describes the contract, since there is no hidden wrapping layer to document separately.

### Negative Consequences

* Breaking change: a 1.x client sending/expecting the old base64/JSON-wrapped format cannot talk to a 2.x server, and vice versa - this is one of the two reasons the major version was bumped to 2.0.0.
* Debugging a raw-bytes request/response with generic HTTP tooling is less immediately readable than a JSON body was - though `DisableOptionsHtmlToPdf.DisableCompress` still allows an uncompressed, JSON-labeled body for manual/curl debugging.

## Links

* Related to [ADR-002](./ADR002V01R01-explicit-render-mode-with-a-url-allow-policy-to-close-ssrf.md) - shipped in the same request-contract generation.
* Related to [ADR-001](./ADR001V01R01-use-structured-error-info-instead-of-raw-exceptions.md) - failures still use the structured JSON `ErrorInfo` contract, only successful `byte[]` output bypasses JSON.
