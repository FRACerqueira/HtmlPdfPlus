// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
    
builder.Services.AddOpenApi();

builder.Services.AddHtmlPdfService((cfg) =>
{
    cfg.Logger(LogLevel.Debug, "MyPDFServer");
});
var app = builder.Build();

var logger = app.Services.GetService<ILogger<Program>>();

//Warmup HtmlPdfServerPlus on startup for better performance from the first request
var WarmupTS = app.WarmupHtmlPdfService();
logger?.LogDebug("HtmlPdfServerPlus ready after {tm}", WarmupTS);

// Configure the HTTP request pipeline.
app.MapOpenApi();

app.UseHttpsRedirection();

app.MapPost("/GeneratePdf", async ([FromServices] IHtmlPdfServer<object, byte[]> PDFserver, [FromBody] byte[] requestclienthtmltopdf, CancellationToken token) =>
{
    var result = await PDFserver.Run(requestclienthtmltopdf, token);
    if (result.IsSuccess)
    {
        // Serve the PDF directly - no JSON envelope, no base64. Transport compression, if
        // enabled on this host, is standard Content-Encoding, not an application-level scheme.
        return Results.File(result.OutputData!, "application/pdf");
    }
    return Results.Json(result.Error, statusCode: result.Error!.Code.ToHttpStatusCode());
})
.Produces(200, typeof(byte[]), "application/pdf")
.Produces<ErrorInfo>(StatusCodes.Status400BadRequest)
.Produces<ErrorInfo>(StatusCodes.Status500InternalServerError)
.Produces<ErrorInfo>(StatusCodes.Status503ServiceUnavailable)
.Produces<ErrorInfo>(StatusCodes.Status504GatewayTimeout);

app.Run();
