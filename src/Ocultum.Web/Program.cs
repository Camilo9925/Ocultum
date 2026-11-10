using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ocultum.Core;
using Ocultum.Core.Validation;
using Ocultum.Infrastructure;
using Ocultum.Web.Errors;
using Ocultum.Web.Hosting;
using Ocultum.Web.Security;

// When published, the executable may be started from any folder: resolve content and settings next to it.
var publishedLayout = Directory.Exists(Path.Combine(AppContext.BaseDirectory, "wwwroot"));
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = publishedLayout ? AppContext.BaseDirectory : null,
});

builder.Services.Configure<LimitsOptions>(builder.Configuration.GetSection(LimitsOptions.SectionName));
builder.Services.Configure<ServerOptions>(builder.Configuration.GetSection(ServerOptions.SectionName));
var limits = builder.Configuration.GetSection(LimitsOptions.SectionName).Get<LimitsOptions>() ?? new LimitsOptions();
var server = builder.Configuration.GetSection(ServerOptions.SectionName).Get<ServerOptions>() ?? new ServerOptions();

// Two covers plus a payload can travel in a single request (lab); leave room for multipart overhead.
var maxRequestBytes = (2 * limits.MaxCoverBytes) + limits.MaxPayloadBytes + (1024 * 1024);

var isTesting = builder.Environment.IsEnvironment("Testing");
var port = server.Port;
if (!isTesting)
{
    port = PortFinder.FindFree(server.Port, server.PortSearchRange);
    builder.WebHost.ConfigureKestrel(kestrel =>
    {
        kestrel.Listen(IPAddress.Loopback, port);
        kestrel.Limits.MaxRequestBodySize = maxRequestBytes;
    });
}

builder.Services.Configure<FormOptions>(form => form.MultipartBodyLengthLimit = maxRequestBytes);

void ConfigureJson(JsonSerializerOptions options)
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
}

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o => ConfigureJson(o.SerializerOptions));
builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(o => ConfigureJson(o.JsonSerializerOptions))
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = context =>
        {
            var problem = ApiExceptionHandler.Create(
                StatusCodes.Status400BadRequest,
                "VALIDATION_FAILED",
                "One or more fields are invalid.");
            problem.Extensions["errors"] = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => JsonNamingPolicy.CamelCase.ConvertName(entry.Key),
                    entry => entry.Value!.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage).ToArray());
            return new ObjectResult(problem)
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ContentTypes = { "application/problem+json" },
            };
        };
    });

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Type = null;
    context.ProblemDetails.Extensions.Remove("traceId");
    if (!context.ProblemDetails.Extensions.ContainsKey("errorCode"))
    {
        context.ProblemDetails.Extensions["errorCode"] = context.ProblemDetails.Status switch
        {
            StatusCodes.Status404NotFound => "NOT_FOUND",
            StatusCodes.Status405MethodNotAllowed => "METHOD_NOT_ALLOWED",
            StatusCodes.Status415UnsupportedMediaType => "UNSUPPORTED_MEDIA_TYPE",
            _ => "ERROR",
        };
    }
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "Ocultum API";
    document.Info.Version = "v1";
    document.Info.Description = "REST wrapper around Steghide 0.5.1: hide and recover data in JPEG, BMP, WAV and AU files.";
    return Task.CompletedTask;
}));

builder.Services.AddOcultumCore();
builder.Services.AddOcultumInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<LocalRequestGuardMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseStaticFiles();
app.MapOpenApi();
app.MapControllers();

if (!isTesting)
{
    var url = $"http://127.0.0.1:{port}";
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Console.WriteLine();
        Console.WriteLine($"  Ocultum esta rodando em {url}");
        Console.WriteLine("  Para encerrar, feche esta janela ou pressione Ctrl+C.");
        Console.WriteLine();

        if (server.OpenBrowser)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                Console.WriteLine($"  Nao foi possivel abrir o navegador. Acesse {url} manualmente.");
            }
        }
    });
}

app.Run();

public partial class Program;
