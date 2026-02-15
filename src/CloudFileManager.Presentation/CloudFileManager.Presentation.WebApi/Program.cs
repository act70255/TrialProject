using CloudFileManager.Presentation.WebApi;
using CloudFileManager.Application.Configuration;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
bool shouldRunMigrations = DependencyRegister.RegisterServices(builder);

builder.Services.AddSwaggerGen(options =>
{
    OpenApiSecurityScheme apiKeyScheme = new()
    {
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Provide API key in X-Api-Key header."
    };

    options.AddSecurityDefinition("ApiKey", apiKeyScheme);
    options.AddSecurityRequirement(document =>
    {
        OpenApiSecuritySchemeReference securitySchemeReference = new("ApiKey", document, string.Empty);
        return new OpenApiSecurityRequirement
        {
            [securitySchemeReference] = []
        };
    });
});

var app = builder.Build();
await DependencyRegister.InitializeInfrastructureAsync(app, shouldRunMigrations);
AppConfig config = app.Services.GetRequiredService<AppConfig>();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        IExceptionHandlerFeature? feature = context.Features.Get<IExceptionHandlerFeature>();
        Exception? exception = feature?.Error;
        ILogger<Program> logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        Program.LogUnhandledException(logger, context.Request.Method, context.Request.Path, exception);

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Unhandled server error",
            Detail = "The server failed to process the request."
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

if (config.UseSwagger)
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
    private static readonly Action<ILogger, string, string, Exception?> LogUnhandledExceptionMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1501, "UnhandledException"),
            "Unhandled exception while processing {Method} {Path}");

    public static void LogUnhandledException(ILogger logger, string method, string path, Exception? exception)
    {
        LogUnhandledExceptionMessage(logger, method, path, exception);
    }
}
