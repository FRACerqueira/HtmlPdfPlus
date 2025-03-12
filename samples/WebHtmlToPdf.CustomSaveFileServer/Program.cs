
// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;
using Microsoft.AspNetCore.Mvc;
using WebHtmlToPdf.CustomSaveFileServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddHtmlPdfService<DataSavePDF,string>((cfg) =>
{
    cfg.Logger(LogLevel.Debug, "MyPDFServer");
});
var app = builder.Build();

var logger = app.Services.GetService<ILogger<Program>>();

//Warmup HtmlPdfServerPlus on startup for better performance from the first request
var WarmupTS = app.WarmupHtmlPdfService<DataSavePDF, string>();
logger?.LogDebug("HtmlPdfServerPlus ready after {tm}", WarmupTS);

// Configure the HTTP request pipeline.
app.MapOpenApi();

app.UseHttpsRedirection();

app.MapPost("/SavePdf", async ([FromServices] IHtmlPdfServer<DataSavePDF,string> PDFserver, [FromBody] string requestclienthtmltopdf, CancellationToken token) =>
{
    return await PDFserver
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
        .Run(requestclienthtmltopdf, token);
}).Produces<HtmlPdfResult<string>>(200);

app.Run();
