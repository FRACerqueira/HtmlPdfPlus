# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
EXPOSE 8080
EXPOSE 8081

# This stage (pre-installed playwright) install the .net 9 runtime  and used to build the service project
FROM mcr.microsoft.com/playwright/dotnet:v1.50.0 AS build  

USER root

# remove browsers and other stuff we don't need
RUN rm -rf /ms-playwright/ffmpeg*
RUN rm -rf /ms-playwright/firefox*
RUN rm -rf /ms-playwright/webkit*
RUN rm -rf /ms-playwright/chromium-*
RUN rm -rf /usr/share/dotnet

# Download the dotnet install script for .net 9	
RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
RUN chmod +x ./dotnet-install.sh

# Run it against a version you want
RUN ./dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet/ 

ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY ["/samples/WebHtmlToPdf.GenericServer/**", "samples/WebHtmlToPdf.GenericServer/"]
COPY ["/src/HtmlPdfShrPlus/**", "HtmlPdfShrPlus/"]
COPY ["/src/HtmlPdfSrvPlus/**", "HtmlPdfSrvPlus/"]

RUN dotnet restore "./samples/WebHtmlToPdf.GenericServer/WebHtmlToPdf.GenericServer.csproj"
COPY . .

WORKDIR "/src/samples/WebHtmlToPdf.GenericServer/"
RUN dotnet build "./WebHtmlToPdf.GenericServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WebHtmlToPdf.GenericServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy required folder(pre-installed) to run playwright
COPY --from=build /ms-playwright/ /ms-playwright

USER root

# Install Google Chrome Stable and fonts
# Note: this installs the necessary libs to make the browser work. 
RUN apt-get update && apt-get install gnupg wget -y && \
    wget --quiet --output-document=- https://dl-ssl.google.com/linux/linux_signing_key.pub | gpg --dearmor > /etc/apt/trusted.gpg.d/google-archive.gpg && \
    sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list' && \
    apt-get update && \
    apt-get install google-chrome-stable -y --no-install-recommends && \
    rm -rf /var/lib/apt/lists/*

# Set emvironment variable for playwright
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

# This stage enables running the service as a non-root user
RUN chown -R $APP_UID /app          

USER $APP_UID           
            
ENTRYPOINT ["dotnet", "WebHtmlToPdf.GenericServer.dll"]
            

