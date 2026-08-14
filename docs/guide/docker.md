![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### Docker

The use of Playwright works very well for local testing on Windows machines following the standard installation instructions.

For containerization scenarios, image size is worth a closer look. The [Dockerfile](../../Dockerfile) launches Chromium with `Headless = true` and no `Channel`, which Playwright always resolves to its own bundled `chromium_headless_shell` binary - so that's the only browser variant the image needs to keep.

**What actually makes the image small:**
- Keep only the one Playwright-bundled browser variant the code launches - `chromium_headless_shell` - and delete the full desktop `chromium`, `firefox`, `webkit` and `ffmpeg` builds that ship alongside it but are never referenced. No separate browser is installed via `apt`.
- The build-stage image tag (`mcr.microsoft.com/playwright/dotnet:v1.62.0`) is kept in lockstep with the `Microsoft.Playwright` NuGet package version referenced by [`HtmlPdfPlus.Server.csproj`](../../src/HtmlPdfPlus.Server/HtmlPdfPlus.Server.csproj) - Playwright requires the browser revision to match its own driver, so bumping that NuGet package without bumping this tag will break the container. **Keep them together.** This coupling is recorded as [ADR-006](../adr/ADR006V01R01-keep-only-chromium-headless-shell-and-couple-its-docker-tag-to-the-nu-get-version.md).
- The final stage's dependency list (`libnss3`, `libatk-bridge2.0-0t64`, etc.) was derived by running `ldd` against the actual `chromium_headless_shell` binary, not copied from generic documentation - Ubuntu Noble's `libasound2` → `libasound2t64` package rename, for example, is easy to get wrong by guessing.
- Both build and runtime stages target the same Ubuntu Noble base (`aspnet:10.0-noble` instead of the Debian-based default `aspnet:10.0`), so the browser binary runs on the same glibc family it was built and validated against.

| | Image size |
|---|---|
| Previous approach | 763 MB |
| Current [Dockerfile](../../Dockerfile) | **370 MB** |

I believe this work can still be improved! **For reference on this approach, see the [Dockerfile](../../Dockerfile)**.

### See Also
* [Main README](../../README.md)
* [Guide index](index.md)
* [ADR-006: single browser variant + Docker/NuGet version coupling](../adr/ADR006V01R01-keep-only-chromium-headless-shell-and-couple-its-docker-tag-to-the-nu-get-version.md)
