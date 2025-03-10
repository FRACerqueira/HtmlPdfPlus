# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/playwright/dotnet:v1.50.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

USER root

# Download the dotnet install script
RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
RUN chmod +x ./dotnet-install.sh

# Run it against a version you want
RUN ./dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet/ --runtime aspnetcore

RUN dotnet nuget locals all --clear

USER $APP_UID

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["/samples/WebHtmlToPdf.DockerGenericServer/**", "samples/WebHtmlToPdf.DockerGenericServer/"]
COPY ["/src/HtmlPdfShrPlus/**", "HtmlPdfShrPlus/"]
COPY ["/src/HtmlPdfSrvPlus/**", "HtmlPdfSrvPlus/"]

RUN dotnet restore "./samples/WebHtmlToPdf.DockerGenericServer/WebHtmlToPdf.DockerGenericServer.csproj"
COPY . .

WORKDIR "/src/samples/WebHtmlToPdf.DockerGenericServer/"
RUN dotnet build "./WebHtmlToPdf.DockerGenericServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WebHtmlToPdf.DockerGenericServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root
RUN chown -R $APP_UID /app
USER $APP_UID

ENTRYPOINT ["dotnet", "WebHtmlToPdf.DockerGenericServer.dll"]