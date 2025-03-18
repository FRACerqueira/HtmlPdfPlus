# ![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png) Welcome to HtmlPdfPlus

### **Lightweight and scalable HTML to PDF converter in .NET.** 

![GitHub license](https://img.shields.io/github/license/fracerqueira/HtmlPdfPlus)
## The best tool to convert HTML to PDF in .NET with a modern engine

[![Build](https://github.com/FRACerqueira/HtmlPdfPlus/workflows/Build/badge.svg)](https://github.com/FRACerqueira/HtmlPdfPlus/actions/workflows/build.yml)
[![Publish](https://github.com/FRACerqueira/HtmlPdfPlus/actions/workflows/publish.yml/badge.svg)](https://github.com/FRACerqueira/HtmlPdfPlus/actions/workflows/publish.yml)

- Client : [![NuGet Client](https://img.shields.io/nuget/v/HtmlPdfPlus.svg)](https://www.nuget.org/packages/HtmlPdfPlus.Client/) [![NuGet Client](https://img.shields.io/nuget/dt/HtmlPdfPlus.svg)](https://www.nuget.org/packages/HtmlPdfPlus.Client/)
- Server : [![NuGet Server](https://img.shields.io/nuget/v/HtmlPdfPlus.svg)](https://www.nuget.org/packages/HtmlPdfPlus.Server/) [![NuGet Server](https://img.shields.io/nuget/dt/HtmlPdfPlus.svg)](https://www.nuget.org/packages/HtmlPdfPlus.Server/)

## Table of Contents

- [Project Description](#project-description)
- [Features](#features)
- [Prerequisites](#prerequisites)
- [Installing](#installing)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [Docker Usage](#docker-usage)
- [Examples](#examples)
- [Documentation](#documentation)
- [Code of Conduct](#code-of-conduct)
- [Contributing](#contributing)
- [Credits](#credits)
- [License](#license)
- [FAQ](#faq)

## Project Description
HtmlPdfPlus is a modern and lightweight library for **.Net9, .Net8 and NetStandard2.1** that allows you to convert HTML or RAZOR pages to PDF with high fidelity. 

It is a scalable and flexible solution that can be used in client-server mode or only server. It supports CSS and JavaScript, and it is easy to integrate with your application. 

You can customize the PDF settings, such as page size and margins, and add headers and footers to your PDF files. HtmlPdfPlus is a powerful tool that can help you generate PDF files from HTML or RAZOR pages with ease.

This library was built using the [Playwright](https://playwright.dev/dotnet/) (engine to automate **Chromium, Firefox, and WebKit** with a single API). Playwright is built to enable cross-browser web automation that is evergreen, capable, reliable, and fast. 

The current version (V.1.50.0) of **Playwright** supports **only the Chromium browser** for the PDF API.

## Features
[**Top**](#table-of-contents)

- Convert HTML or RAZOR page to PDF with high fidelity
- Support for CSS and JavaScript
- Asynchronous API
- Customizable PDF settings (e.g., page size, margins)
- Support for headers and footers
- Lightweight and easy to integrate 
- Flexible and scalable (Client-Server mode or only Server)
- Support HTML5 and CSS3
- Communicate with the server using REST API (with compressed request) or user custom protocol
- Minify HTML and CSS
- Client-side HTML parser with custom error action (optional)
- Compress PDF using GZip over network (Only type bytes array output)
- Extension on server side to customize the conversion process (before and after conversion)
    - BeforePDF : Normalize HTML, Replace tokens, etc
    - AfterPDF : Save file, Send to cloud, etc
- Disable features to improve/ balance performance (minify, compress and log)

### What's new in the latest version 

- **v0.3.0-beta (latest version)**
    - Added FromUrl(Uri value) command to client-side mode
    - Fixed bug in server mode for multi thread safe when there is parameter customization and/or no client mode sending.
        - Moved the BeforePDF(Func<string, TIn?, CancellationToken, Task<string>> inputParam) command to the execution context.
        - Moved the AfterPDF(Func<byte[]?, TIn?, CancellationToken, Task<TOut>> outputParam) command to the execution context.
        - Added command Source(TIn? inputparam = default) to transfer input parameter for server execution context and custom actions and html source.
        - Added Request(string request Client) command to pass the request client data to the server execution context for custom actions and HTML source.
        - Simplified execution commands for server side with execution context with fluid interface comands :
            -  Removed static class RequestHtmlPdf
            -  Added command FromHtml(string html, int converttimeout = 30000, bool minify = true)
            -  Added command FromUrl(Uri value, int converttimeout = 30000)
            -  Added command FromRazor\<T\>(string template, T model, int converttimeout = 30000, bool minify = true)
- **v0.2.0-beta**
    - Initial version

## Prerequisites
[**Top**](#table-of-contents)

- .NET 8 or .NET 9 SDK
- Visual Studio 2022 or later
- Playwright (Installed and configured for your O.S)


## Installing
[**Top**](#table-of-contents)

### Installation Steps for Playwright (Windows)

```
dotnet tool update --global PowerShell
dotnet tool install --global Microsoft.Playwright.CLI
playwright.exe install --with-deps
```

_Note: Make sure that the path to the executable is mapped to: C:\Users\\[login]\\.dotnet\tools._

_If it is not, run it directly via the path C:\Users\\[login]\\.dotnet\tools\playwright.exe install --with-deps_

### Installation Steps for HtmlPdfPlus

**Client library** can be installed via NuGet or line command. 
```
Install-Package HtmlPdfPlus.Client [-pre]
```

```
dotnet add package HtmlPdfPlus.Client [--prerelease]
```

**Server library** can be installed via NuGet or line command. 


```
Install-Package HtmlPdfPlus.Server [-pre]
```

```
dotnet add package HtmlPdfPlus.Server [--prerelease]
```

**_Note:  [-pre]/[--prerelease] usage for pre-release versions_**

## Getting Started
[**Top**](#table-of-contents)

Follow these steps to get started with HtmlPdfPlus:

1. Install the necessary packages using NuGet.
2. Configure the services in your application.
3. Use the provided API to convert HTML to PDF.

## Usage
[**Top**](#table-of-contents)

It is possible to generate a PDF in two ways:

### 1) Using client-server mode

#### 1.1) Via http

![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/swimlanes.io.Http.png)

#### basic usage client side

```csharp
using HtmlPdfPlus;

...
Host.CreateDefaultBuilder(args).ConfigureServices((hostContext, services) =>
{ 
   services.AddHttpClient("HtmlPdfServer", httpClient =>
   {
      httpClient.BaseAddress = new Uri("https://localhost:7212/GeneratePdf");
   });
});
...

//client http to endpoint    
var clienthttp = HostApp!.Services
	.GetRequiredService<IHttpClientFactory>()
	.CreateClient("HtmlPdfServer");

//create client instance and send to HtmlPdfPlus server endpoint    
var pdfresult = await HtmlPdfClient.Create("HtmlPdfPlusClient")
    .PageConfig((cfg) => cfg.Margins(10))
    .FromHtml(HtmlSample())
    .Timeout(5000)
    .Run(clienthttp, token);

//performs writing to file after performing conversion
if (pdfresult.IsSuccess)
{
    await File.WriteAllBytesAsync("html2pdfsample.pdf", pdfresult.OutputData!);
}
else
{
    //show error via pdfresult.Error
}
```

#### basic usage Server side

```csharp
using HtmlPdfPlus;

...
var builder = WebApplication.CreateBuilder(args);  
builder.Services.AddHtmlPdfService((cfg) =>
{
    cfg.Logger(LogLevel.Debug, "MyPDFServer");
});
...

app.MapPost("/GeneratePdf", async ([FromServices] IHtmlPdfServer<object, byte[]> PDFserver, [FromBody] string requestclienthtmltopdf, CancellationToken token) =>
{
    return await PDFserver.Run(requestclienthtmltopdf, token);
}).Produces<HtmlPdfResult<byte[]>>(200);

```


#### 1.2) Via any process

![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/swimlanes.io.AnyProcess.png)

#### basic usage client side

```csharp
using HtmlPdfPlus;

//create client instance and send to HtmlPdfPlus server endpoint    
var pdfresult = await HtmlPdfClient.Create("HtmlPdfPlusClient")
    .PageConfig((cfg) => cfg.Margins(10))
    .FromHtml(HtmlSample())
    .Timeout(5000)
    .Run(SendToServer, token);

//performs writing to file after performing conversion
if (pdfresult.IsSuccess)
{
    await File.WriteAllBytesAsync("html2pdfsample.pdf", pdfresult.OutputData!);
}
else
{
    //show error via pdfresult.Error
}

private static async Task<HtmlPdfResult<byte[]>> SendToServer(string requestdata, CancellationToken token)
{
   //send requestdata to server and return result
}

```

#### basic usage Server side

```csharp
using HtmlPdfPlus;

...
var builder = WebApplication.CreateBuilder(args);  
builder.Services.AddHtmlPdfService((cfg) =>
{
    cfg.Logger(LogLevel.Debug, "MyPDFServer");
});
...
var PDFserver = HostApp.Services.GetHtmlPdfService();

var result = await PDFserver.Run(requestdata , Token);

//send result to client

```

### 2) Using ony-server

![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/swimlanes.io.OnlyServer.png)

#### basic usage
```csharp
using HtmlPdfPlus;

...
Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHtmlPdfService((cfg) =>
        {
               .Logger(LogLevel.Debug, "MyPDFServer")
               .DefaultConfig((page) =>
               {
                   page.DisplayHeaderFooter(true)
                       .Margins(10, 10, 10, 10);
               });
        });
    });
...

//instance of Html to Pdf Engine and Warmup HtmlPdfServerPlus
var PDFserver = HostApp!.Services.GetHtmlPdfService();

//Performs conversion and custom operations on the server
var pdfresult = await PDFserver
       .Source()
       .FromHtml(HtmlSample(),5000)
       .Run(applifetime.ApplicationStopping);

//performs writing to file after performing conversion
if (pdfresult.IsSuccess)
{
    await File.WriteAllBytesAsync( "html2pdf.pdf", pdfresult.OutputData!);
}
else
{
    //show error via pdfresult.Error
}
```

# Docker Usage
[**Top**](#table-of-contents)

The use of Playwright works very well for local testing on Windows machines following the standard installation instructions.

For containerization scenarios, image sizes are a challenge that deserves more dedicated attention.

This project suggests a containerization example that **reduces the final image size by approximately 70% !.** 


To achieve this reduction, the biggest challenge was controlling the necessary dependencies and keeping only the minimum for execution in a headless shell.

Basically, what we did was:
- Use the base image from mcr.microsoft.com/dotnet/aspnet:9.0
- Use the image from cr.microsoft.com/playwright/dotnet:v1.50.0 for build
  - Removing unnecessary browser and driver installations
  - For .NET 9, we removed the default installation (.NET 8)
    - We installed the .NET 9 SDK version for the build phase
- Copy the required folder (pre-installed) to run Playwright
- Install Google Chrome Stable , fonts and install the necessary libs to make the browser work.
- Set environment variable for Playwright
- Enable running the service as a non-root user

I believe this work can still be improved! **For reference on this approach, see the [DockFile](./Dockerfile)**.


## Examples
[**Top**](#table-of-contents)

For more examples, please refer to the [Samples directory](./samples) :

- Server Only
	- [Console HtmlToPdfPlus OnlyAtServer V1](./samples/ConsoleHtmlToPdfPlus.OnlyAtServer/v1)
        - Performs replacement token substitution in the HTML source before performing the conversion 
        - Performs writing to file after performing conversion
        - Return output data with filename
	- [Console HtmlToPdfPlus OnlyAtServer V2](./samples/ConsoleHtmlToPdfPlus.OnlyAtServer/v2)
        - Performs generate pdf in bytes array
        - Performs writing to file
- Client-Server
	- [Console HtmlToPdfPlus Client by Http](./samples/ConsoleHtmlToPdfPlus.ClientSendHttp)
        - Performs sending data to the server via http client
        - Performs writing to file
	- [Server HtmlToPdfPlus Generic](./samples/WebHtmlToPdf.GenericServer)
        - Performs generate pdf in bytes array
        - Send data to client via http
- Client-Server Custom
	- [Console HtmlToPdfPlus Client Custom by Http](./samples/ConsoleHtmlToPdfPlus.ClientCustomSendHttp)
        - Performs a generic suggestion for writing a file to a cloud like gcp/azure   
        - Performs sending data to the server via http client
	- [Server HtmlToPdfPlus Custom Save File](./samples/WebHtmlToPdf.CustomSaveFileServer)
        - Performs replacement token substitution in the HTML source before performing the conversion 
        - Performs a generic suggestion writing to file after performing conversion
        - Send data (name of file or full path file) to client via http
- Client-Server TCP
	- [Console HtmlToPdfPlus Client Tcp](./samples/ConsoleHtmlToPdfPlus.ClientSendTcp)
        - Performs sending data to the server via tcp client (using [SuperSimpleTcp](https://github.com/jchristn/SuperSimpleTcp) package)
        - Performs receiver data from the server via tcp client
        - Performs writing to file
    - [Server Console HtmlToPdfPlus Tcp](./samples/TcpServerHtmlToPdf.GenericServer)
        - Listening port on tcp server (using [SuperSimpleTcp](https://github.com/jchristn/SuperSimpleTcp) package)
        - Performs generate pdf in bytes array
        - Send data to client via tcp server
 
## Documentation
[**Top**](#table-of-contents)

The library is well documented and has a main namespace `HtmlPdfPlus` for client and server, and all methods use fluent interface. 

The documentation is available in the [Docs directory](./src/docs/docindex.md).

## Code of Conduct
[**Top**](#table-of-contents)

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.
For more information see the [Code of Conduct](CODE_OF_CONDUCT.md).

## Contributing
[**Top**](#table-of-contents)

Please read [Contributing](CONTRIBUTING.md) for details on our code of conduct, and the process for submitting pull requests to us.

## Credits
[**Top**](#table-of-contents)

**API documentation generated by**

- [XmlDocMarkdown](https://github.com/ejball/XmlDocMarkdown), Copyright (c) 2024 [Ed Ball](https://github.com/ejball)
    - See an unrefined customization to contain header and other adjustments in project [XmlDocMarkdownGenerator](./src/XmlDocMarkdownGenerator)  
    
## License
[**Top**](#table-of-contents)

This project is licensed under the MIT License - see the [License](LICENSE.md) file for details.

**Disclaimer** : HtmlPdfPlus **<u>includes PackageReference</u>** from other software released under other licences:

- [NUglify](https://github.com/trullock/NUglify) released under the [BSD-Clause 2 license](http://opensource.org/licenses/BSD-2-Clause).
   - The original Microsoft Ajax Minifier was released under the [Apache 2.0 license](http://www.apache.org/licenses/LICENSE-2.0).

## FAQ
[**Top**](#table-of-contents)

**Q: What browsers are supported for PDF generation?**

A: Currently, only the Chromium browser is supported for the PDF API.

**Q: What init args for speed and reduce resource usage ?**

A: Currently, HtmlPdfPlus.Server starts with "--run-all-compositor-stages-before-draw --disable-dev-shm-usage -disable-setuid-sandbox --no-sandbox" when no argument value is passed.

**Q: Can I customize the PDF settings?**

A: Yes, you can customize settings such as page size, margins, headers, and footers.

**Q: Is there support for asynchronous operations?**

A: Yes, the API supports asynchronous operations.

**Q: How can I contribute to the project?**

A: Please refer to the [Contributing](CONTRIBUTING.md) section for details on how to contribute.
