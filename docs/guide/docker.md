![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### Docker

The use of Playwright works very well for local testing on Windows machines following the standard installation instructions.

For containerization scenarios, image sizes are a challenge that deserves more dedicated attention.

> **2026-08 correction.** The [Dockerfile](../../Dockerfile) had been broken since 2025-11-13 (commit `93be7db`): a malformed Chrome download URL made the image fail to build at all, so the "~70% smaller" claim previously made here described an image nobody could actually produce. Investigating it surfaced a second, more important problem: the code launches Chromium with `Headless = true` and no `Channel`, which Playwright always resolves to its own bundled `chromium_headless_shell` binary - **the separately apt-installed Google Chrome was never used**, at any point. It only ever added dead weight. The Dockerfile has been rewritten around this; see below for what changed and the real, measured numbers.

**What actually makes the image small now:**
- Keep only the one Playwright-bundled browser variant the code launches - `chromium_headless_shell` - and delete the full desktop `chromium`, `firefox`, `webkit` and `ffmpeg` builds that ship alongside it but are never referenced. No separate browser is installed via `apt`.
- The build-stage image tag (`mcr.microsoft.com/playwright/dotnet:v1.62.0`) is kept in lockstep with the `Microsoft.Playwright` NuGet package version referenced by [`HtmlPdfPlus.Server.csproj`](../../src/HtmlPdfPlus.Server/HtmlPdfPlus.Server.csproj) - a mismatch here is exactly what caused the container to crash on startup during this investigation (Playwright refuses to launch a browser revision that doesn't match its own driver, and says so explicitly in the exception). **Bumping that NuGet package without bumping this tag will break the container the same way - keep them together.** This coupling is recorded as [ADR-006](../adr/ADR006V01R01-keep-only-chromium-headless-shell-and-couple-its-docker-tag-to-the-nu-get-version.md).
- The final stage's dependency list (`libnss3`, `libatk-bridge2.0-0t64`, etc.) was derived by running `ldd` against the actual `chromium_headless_shell` binary, not copied from generic documentation - Ubuntu Noble's `libasound2` → `libasound2t64` package rename, for example, is easy to get wrong by guessing.
- Both build and runtime stages target the same Ubuntu Noble base (`aspnet:10.0-noble` instead of the Debian-based default `aspnet:10.0`), so the browser binary runs on the same glibc family it was built and validated against.

**Measured, not estimated:** built and booted both versions end-to-end (container start, page-pool warmup, a real `POST /GeneratePdf` producing a valid PDF) to confirm correctness before comparing size.

| | Image size |
|---|---|
| Previous approach, with only the broken URL fixed (still boots into a crash) | 763 MB |
| Current [Dockerfile](../../Dockerfile) | **370 MB** |

I believe this work can still be improved! **For reference on this approach, see the [Dockerfile](../../Dockerfile)**.

### See Also
* [Main README](../../README.md)
* [ADR-006: single browser variant + Docker/NuGet version coupling](../adr/ADR006V01R01-keep-only-chromium-headless-shell-and-couple-its-docker-tag-to-the-nu-get-version.md)
