<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values Migrated |
|--|--|
|File title md|Keep only chromium headless shell and couple its Docker tag to the NuGet version|
|Version|01|
|Revision|01|
|Scope||
|Domain||
|Created|Proposed (2026-08-14)|
|Changed|Accepted (2026-08-14)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Keep only chromium_headless_shell and couple its Docker tag to the NuGet version

## Deciders

* Deciders: Fernando Cerqueira

Technical Story: commit `c5d323d` - repair broken Dockerfile and cut image size 763MB -> 370MB

## Context and Problem Statement

The Dockerfile had been broken since 2025-11-13 (a malformed Chrome download URL made the image fail to build at all). Investigating the fix surfaced a deeper issue: the code launches Chromium with `Headless = true` and no `Channel`, which Playwright always resolves to its own bundled `chromium_headless_shell` binary - a separately apt-installed Google Chrome, present in the previous Dockerfile, was never used at any point; it only added dead weight. Separately, the build-stage Playwright image tag and the `Microsoft.Playwright` NuGet package version must describe the exact same browser revision, or the container crashes on startup - Playwright refuses to launch a browser revision that doesn't match its own driver. How should the image be built to be both minimal and correct?

## Decision Drivers

* Container image size is a direct cost (registry storage, pull time, cold-start time) that should reflect what the code actually uses, not what might theoretically be useful.
* A version mismatch between the NuGet package and the Docker build-stage tag is a silent, unfixed-by-testing failure mode - it doesn't show up until the container tries to launch a browser.
* The final runtime dependency list should be derived from what the binary actually needs, not copied from generic documentation that can be stale (e.g. Ubuntu Noble's `libasound2` -> `libasound2t64` package rename).

## Considered Options

* Keep installing a separate system Chrome via apt, alongside Playwright's bundled browsers
* Keep every Playwright-bundled browser variant (chromium, chromium_headless_shell, firefox, webkit, ffmpeg)
* Keep only `chromium_headless_shell` (the one variant the code launches), and pin the build-stage image tag to the NuGet package version with an explicit comment

## Decision Outcome

Chosen option: "Keep only `chromium_headless_shell`, and pin the build-stage image tag to the NuGet package version with an explicit comment", because it is the only option that matches the image contents to what the code actually runs. Deleting every browser directory except `chromium_headless_shell-*` after the build stage, and never installing a separate system Chrome, cut the working image from 763MB to 370MB with no behavior change (verified end-to-end: container boot, page-pool warmup, a real `POST /GeneratePdf` producing a valid PDF). The version coupling is documented directly in the Dockerfile's own comments at both the `FROM mcr.microsoft.com/playwright/dotnet:vX.Y.Z` line and the `.csproj` `PackageReference`, so the two are visibly one decision, not two independent version bumps.

### Positive Consequences

* Image size tracks what the code actually uses, not a maximal Playwright install.
* The version-coupling comment turns a previously silent failure mode (crash on container start) into something a reviewer can check by reading two adjacent lines.
* The runtime `apt` dependency list, derived from `ldd` against the actual binary, is verified rather than guessed - avoiding stale package names like the Ubuntu Noble `t64` rename.

### Negative Consequences

* Bumping the `Microsoft.Playwright` NuGet package version now requires a matching, deliberate edit to the Dockerfile's build-stage tag - a step that is easy to forget without the comment, and nothing currently enforces it automatically (e.g. no CI check diffing the two versions).
* If the code ever needs a browser other than `chromium_headless_shell` (e.g. an explicit `Channel` or a non-headless run), the Dockerfile's browser-pruning step must be revisited - it is coupled to current code behavior, not a general-purpose Playwright image.

## Pros and Cons of the Options

### Keep a separate system Chrome via apt

* Good, because it matches a common Playwright deployment pattern found in generic documentation.
* Bad, because the code never references it - verified by reading the browser-launch call site - so it is pure dead weight.

### Keep every Playwright-bundled browser variant

* Good, because it requires no pruning logic and supports any future `Channel`/headed-mode change with no Dockerfile edit.
* Bad, because it ships firefox, webkit and full desktop chromium binaries that the current code path never launches, inflating the image for capability that isn't used.

### Keep only `chromium_headless_shell`, pin the version coupling explicitly

* Good, because it matches the image to actual code behavior and is the smallest correct option.
* Good, because the version-coupling comment makes a previously silent failure mode visible at review time.
* Bad, because it must be revisited if the code's browser-launch configuration changes.

## Links

* See [docs/guide/docker.md](../guide/docker.md) for the measured before/after image sizes and the full dependency-derivation process.
