
// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using WebHtmlToPdf.CustomSaveFileServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddHtmlPdfService<DataSavePDF,string>((cfg) =>
{
    cfg.Logger(LogLevel.Debug, "MyPDFServer");
});
var app = builder.Build();

var logger = app.Services.GetService<ILogger<Program>>();

var WarmupTS = app.WarmupHtmlPdfService<DataSavePDF, string>();
logger?.LogDebug("HtmlPdfServerPlus ready after {tm}", WarmupTS);

// Configure the HTTP request pipeline.
app.MapOpenApi();

app.UseHttpsRedirection();

app.MapHtmlPdfEndpoints<DataSavePDF, string>(
    "/SavePdf",
    beforePdf: (html, inputparam, _) =>
    {
        if (inputparam is null)
        {
            return Task.FromResult(html);
        }
        var aux = html.Replace("[{FileName}]", inputparam.Filename);
        return Task.FromResult(aux);
    },
    afterPdf: (pdfbyte, inputparam, token) =>
    {
        if (inputparam is null)
        {
            return Task.FromResult(string.Empty);
        }
        //TODO : performs writing to file  after performing conversion
        return Task.FromResult(inputparam.Filename);
    });

// The non-generic MapHtmlPdfHealthEndpoints() targets IHtmlPdfServer<object, byte[]> only -
// this host registered <DataSavePDF, string> above, so the matching generic overload is
// required here too.
app.MapHtmlPdfHealthEndpoints<DataSavePDF, string>();

app.Run();
