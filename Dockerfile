# IMPORTANT: this tag must match the Microsoft.Playwright NuGet package version referenced
# by src/HtmlPdfPlus.Server/HtmlPdfPlus.Server.csproj (currently 1.62.0). A mismatch here is
# exactly what breaks the browser at container startup - Playwright refuses to launch a
# browser revision that doesn't match the installed NuGet driver, and says so explicitly.
FROM mcr.microsoft.com/playwright/dotnet:v1.62.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY ["/samples/WebHtmlToPdf.GenericServer/**", "samples/WebHtmlToPdf.GenericServer/"]
COPY ["/src/HtmlPdfPlus.Shared/**", "HtmlPdfShrPlus/"]
COPY ["/src/HtmlPdfPlus.Server/**", "HtmlPdfSrvPlus/"]

RUN dotnet restore "./samples/WebHtmlToPdf.GenericServer/WebHtmlToPdf.GenericServer.csproj"
COPY . .

WORKDIR "/src/samples/WebHtmlToPdf.GenericServer/"
RUN dotnet build "./WebHtmlToPdf.GenericServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# The code launches Chromium with Headless = true and no Channel, which Playwright resolves
# to chromium_headless_shell - never the full desktop "chromium" browser, and never firefox/
# webkit. Keeping only that one browser is why this image is smaller, not because of any
# separately-installed system Chrome (the previous Dockerfile installed Google Chrome via
# apt, but nothing in the code ever referenced it - it was dead weight).
RUN find /ms-playwright -mindepth 1 -maxdepth 1 -type d ! -name 'chromium_headless_shell-*' -exec rm -rf {} +

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WebHtmlToPdf.GenericServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage. Same Ubuntu Noble base as the build stage's Playwright image (rather than
# the Debian-based default aspnet:10.0 tag), so the chromium_headless_shell binary - built
# and tested by the Playwright team against Ubuntu - runs against the same glibc family it
# was validated on.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
EXPOSE 8080
EXPOSE 8081

USER root

# The exact shared-library closure chromium_headless_shell needs, determined by running ldd
# against the actual binary rather than copied from a generic dependency list - Ubuntu
# Noble's libc6 "t64" package rename (e.g. libasound2 -> libasound2t64) means older
# documented package names for this list will fail to resolve.
RUN apt-get update && apt-get install -y --no-install-recommends \
      libglib2.0-0t64 libnspr4 libnss3 libatk1.0-0t64 libatk-bridge2.0-0t64 \
      libdbus-1-3 libx11-6 libxcomposite1 libxdamage1 libxext6 libxfixes3 \
      libxrandr2 libgbm1 libexpat1 libxcb1 libxkbcommon0 libasound2t64 libatspi2.0-0t64 \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=publish /app/publish .

# Only the one browser variant the code actually launches - see the build-stage comment.
COPY --from=build /ms-playwright/ /ms-playwright

ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

# This stage enables running the service as a non-root user
RUN chown -R $APP_UID /app
USER $APP_UID

ENTRYPOINT ["dotnet", "WebHtmlToPdf.GenericServer.dll"]
