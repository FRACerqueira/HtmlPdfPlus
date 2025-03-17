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

app.MapPost("/GeneratePdf", async ([FromServices] IHtmlPdfServer<object, byte[]> PDFserver, [FromBody] string requestclienthtmltopdf, CancellationToken token) =>
{
    return await PDFserver
        .Run(requestclienthtmltopdf,token);
}).Produces<HtmlPdfResult<byte[]>>(200);

app.Run();