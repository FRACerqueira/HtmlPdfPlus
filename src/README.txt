==================================================================================
  _   _   _                 _     ____       _    __     ____    _               
 | | | | | |_   _ __ ___   | |   |  _ \   __| |  / _|   |  _ \  | |  _   _   ___ 
 | |_| | | __| | '_ ` _ \  | |   | |_) | / _` | | |_    | |_) | | | | | | | / __|
 |  _  | | |_  | | | | | | | |   |  __/ | (_| | |  _|   |  __/  | | | |_| | \__ \
 |_| |_|  \__| |_| |_| |_| |_|   |_|     \__,_| |_|     |_|     |_|  \__,_| |___/

==================================================================================

The best tool to convert HTML to PDF in .NET with a modern engine

Project Description
===================

HtmlPdfPlus is a modern and lightweight library for **.Net9, .Net8 and NetStandard2.1** that allows you to convert HTML or RAZOR pages to PDF with high fidelity. 
It is a scalable and flexible solution that can be used in client-server mode or only server. 
It supports CSS and JavaScript, and it is easy to integrate with your application. 
You can customize the PDF settings, such as page size and margins, and add headers and footers to your PDF files. 
HtmlPdfPlus is a powerful tool that can help you generate PDF files from HTML or RAZOR pages with ease.
This library was built using the Playwright (https://playwright.dev/dotnet/) (engine to automate Chromium, Firefox, and WebKit** with a single API). 
Playwright is built to enable cross-browser web automation that is evergreen, capable, reliable, and fast.

The current version (V.1.50.0) of **Playwright** supports **only the Chromium browser** for the PDF API.

Features
========

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

What's new in the latest version 
================================

- v0.4.0-beta (latest version)

    - Relaxation of Package Reference for .net8 to .net9
    - Renamed the 'Source' command to 'Scope'
    - Renamed the 'Request' command to 'ScopeRequest'

- v0.3.0-beta

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
         
- v0.2.0-beta

    - Initial version

Prerequisites
=============

- .NET 8 or .NET 9 SDK
- Visual Studio 2022 or later
- Playwright (Installed and configured for your O.S)

Installation Steps for Playwright (Windows)
===========================================

dotnet tool update --global PowerShell
dotnet tool install --global Microsoft.Playwright.CLI
playwright.exe install --with-deps

Note: Make sure that the path to the executable is mapped to: C:\Users\[login]\.dotnet\tools.
If it is not, run it directly via the path C:\Users\[login]\.dotnet\tools\playwright.exe install --with-deps

Usage
=====

1.0) Using client-server mode Via http

-----------------------------------------
CLIENT SIDE
-----------------------------------------

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

-----------------------------------------
SERVER SIDE
-----------------------------------------

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


1.2) Using client-server mode Via any protocol

-----------------------------------------
CLIENT SIDE
-----------------------------------------

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

-----------------------------------------
SERVER SIDE
-----------------------------------------

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

2.0) Using ony-server

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
       .ScopeData()
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

Samples
=======

For more examples, please refer to the Samples directory : https://github.com/FRACerqueira/HtmlPdfPlus/tree/docs/samples

Docker Usage
============

The use of Playwright works very well for local testing on Windows machines following the standard installation instructions.
For containerization scenarios, image sizes are a challenge that deserves more dedicated attention.
This project suggests a containerization example that reduces the final image size by approximately **70%** !.

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

I believe this work can still be improved! 

For reference on this approach, see the DockFile at: https://github.com/FRACerqueira/HtmlPdfPlus/blob/main/Dockerfile

Documentation
=============

The library is well documented and has a main namespace `HtmlPdfPlus` for client and server, and all methods use fluent interface. 
The documentation is available in the Docs directory : https://github.com/FRACerqueira/HtmlPdfPlus/blob/main/src/docs/docindex.md






  
                                                                                                                    
                                                                                                                    
                                                                                                                    
                                                                                                                    
                                                                                                                    
                                                                                                                    
                                                                                                                    
