// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

var builder = WebApplication.CreateBuilder(args);
    
builder.Services.AddOpenApi();

builder.Services.AddHtmlPdfService((cfg) =>
{
    cfg.Logger(LogLevel.Debug, "MyPDFServer");
});
var app = builder.Build();

var logger = app.Services.GetService<ILogger<Program>>();

var WarmupTS = app.WarmupHtmlPdfService();
logger?.LogDebug("HtmlPdfServerPlus ready after {tm}", WarmupTS);

// Configure the HTTP request pipeline.
app.MapOpenApi();

app.UseHttpsRedirection();

// The request/response contract (raw PDF on success, structured ErrorInfo on failure) comes
// straight from the library, so the OpenAPI document generated above actually describes it.
app.MapHtmlPdfEndpoints("/GeneratePdf");

// Lets an orchestrator (Kubernetes, etc.) observe renderer health from outside instead of
// inferring it from request timeouts.
app.MapHtmlPdfHealthEndpoints();

app.Run();
