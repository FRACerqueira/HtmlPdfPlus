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

HtmlPdfPlus is a modern and lightweight library for **.Net10,.Net9 and .Net8** that allows you to convert HTML or RAZOR pages to PDF with high fidelity. 
It is a scalable and flexible solution that can be used in client-server mode or only server. 
It supports CSS and JavaScript, and it is easy to integrate with your application. 
You can customize the PDF settings, such as page size and margins, and add headers and footers to your PDF files. 
HtmlPdfPlus is a powerful tool that can help you generate PDF files from HTML or RAZOR pages with ease.
This library was built using the Playwright (https://playwright.dev/dotnet/) (engine to automate Chromium, Firefox, and WebKit** with a single API). 
Playwright is built to enable cross-browser web automation that is evergreen, capable, reliable, and fast.

As of the Playwright version this library currently targets, its PDF generation API supports **only the Chromium browser**.

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
- Requests are sent as gzip-compressed raw bytes by default, no base64/JSON-string wrapping
- A successful byte[] response is served as the raw PDF body, not wrapped in JSON
- Extension on server side to customize the conversion process (before and after conversion)
    - BeforePDF : Normalize HTML, Replace tokens, etc
    - AfterPDF : Save file, Send to cloud, etc
- Disable features to improve/balance performance (minify, compress and log)
- Backpressure signaled via ErrorCode.PoolExhausted + a real Retry-After, automatic browser recovery, liveness/readiness endpoints, and System.Diagnostics.Metrics instrumentation

What's new
==========
Current version: 2.0.0. Full version history: https://github.com/FRACerqueira/HtmlPdfPlus/blob/main/CHANGELOG.md

Prerequisites
=============

- .NET 8, .NET 9 or .NET 10 SDK
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
var pdfresult = await HtmlPdfClient
    .Create("HtmlPdfPlusClient")
    .PageConfig((cfg) =>
    {
       cfg.Margins(10)
          .Footer("'<span style=\"text-align: center;width: 100%;font-size: 10px\"> <span class=\"pageNumber\"></span> of <span class=\"totalPages\"></span></span>")
          .Header("'<span style=\"text-align: center;width: 100%;font-size: 10px\" class=\"title\"></span>")
          .Orientation(PageOrientation.Landscape)
          .DisplayHeaderFooter(true);
     })
     .Logger(HostApp.Services.GetService<ILogger<Program>>())
     .FromHtml(HtmlSample())
     .Timeout(5000)
     .Run(clienthttp, applifetime.ApplicationStopping);

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

app.MapHtmlPdfEndpoints();


1.2) Using client-server mode Via any protocol

-----------------------------------------
CLIENT SIDE
-----------------------------------------

using HtmlPdfPlus;

// Generic suggestion for writing a file to a cloud like gcp/azure
// Suggested return would be the full path "repo/filename"
var paramTosave = new DataSavePDF("Filename.pdf","MyRepo","MyConnectionstring");

var pdfresult = await HtmlPdfClient.Create("HtmlPdfPlusClient")
      .PageConfig((cfg) =>
      {
         cfg.Margins(10);
      })
      .Logger(HostApp.Services.GetService<ILogger<Program>>())
      .FromRazor(TemplateRazor(), order1)
      .Timeout(50000)
      .Run<DataSavePDF,string>(SendToServer,paramTosave, applifetime.ApplicationStopping);

//Shwo result
if (pdfresult.IsSuccess)
{
   Console.WriteLine($"File PDF generate at {pdfresult.OutputData}");
}
else
{
    Console.WriteLine($"HtmlPdfClient error: {pdfresult.Error!}");
}

private static async Task<HtmlPdfResult<string>> SendToServer(byte[] requestdata, CancellationToken token)
{
   //send requestdata to server and return result
}

-----------------------------------------
SERVER SIDE
-----------------------------------------

using HtmlPdfPlus;

...
var builder = WebApplication.CreateBuilder(args);  
builder.Services.AddHtmlPdfService<DataSavePDF,string>((cfg) =>
{
    cfg.Logger(LogLevel.Debug, "MyPDFServer");
});
...
var PDFserver = HostApp.Services.GetHtmlPdfService();

var result = await PDFserver
        .ScopeRequest(data)
        .BeforePDF( (html,inputparam, _) =>
        {
            if (inputparam is null)
            {
                return Task.FromResult(html);
            }
            //performs replacement token substitution in the HTML source before performing the conversion
            var aux = html.Replace("[{FileName}]", inputparam.Filename);
            return Task.FromResult(aux);
        })
        .AfterPDF( (pdfbyte, inputparam, token) =>
        {
            if (inputparam is null)
            {
                return Task.FromResult(string.Empty);
            }
            //TODO : performs writing to file  after performing conversion
            return Task.FromResult(inputparam.Filename);
        })
        .Run(token);

//send result to client

2.0) Using ony-server

using HtmlPdfPlus;

...
Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHtmlPdfService((cfg) =>
        {
            cfg.Logger(LogLevel.Debug, "MyPDFServer")
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

For more examples, please refer to the Samples directory : https://github.com/FRACerqueira/HtmlPdfPlus/tree/main/samples

Docker Usage
============

The working Dockerfile keeps only the one Playwright browser variant the code actually launches (chromium_headless_shell),
cutting the image from 763MB to 370MB. Full details, measured numbers, and the version-coupling caveat between the
Docker build-stage tag and the Microsoft.Playwright NuGet package: https://github.com/FRACerqueira/HtmlPdfPlus/blob/main/docs/guide/docker.md

Resilience and Observability
=============================

Backpressure/Retry-After, automatic browser recovery, liveness/readiness endpoints, and metrics:
https://github.com/FRACerqueira/HtmlPdfPlus/blob/main/docs/guide/resilience.md

Architecture Decision Records
==============================

Decisions with a live consequence (error contract, SSRF policy, wire format, backpressure, metrics, Docker/NuGet version
coupling) are recorded as ADRs: https://github.com/FRACerqueira/HtmlPdfPlus/blob/main/docs/adr/indexadrs.md

Documentation
=============

The library is well documented and has a main namespace `HtmlPdfPlus` for client and server, and all methods use fluent interface.
The documentation is available in the Docs directory : https://github.com/FRACerqueira/HtmlPdfPlus/blob/main/docs/api/docindex.md






  
                                                                                                                    
                                                                                                                    
                                                                                                                    
                                                                                                                    
                                                                                                                    
                                                                                                                    
                                                                                                                    
