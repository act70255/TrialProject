using CloudFileManager.Presentation.WebApi;
using CloudFileManager.Application.Configuration;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
bool shouldRunMigrations = DependencyRegister.RegisterServices(builder);
string apiSecurityHeaderName = builder.Configuration["ApiSecurity:HeaderName"] ?? "X-Api-Key";
bool swaggerAutoAuthorize = builder.Configuration.GetValue<bool?>("ApiSecurity:SwaggerAutoAuthorize") ?? true;

builder.Services.AddSwaggerGen(options =>
{
    OpenApiSecurityScheme apiKeyScheme = new()
    {
        Name = apiSecurityHeaderName,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = $"Provide API key in {apiSecurityHeaderName} header."
    };

    options.AddSecurityDefinition("ApiKey", apiKeyScheme);
    options.AddSecurityRequirement(_ =>
    {
        OpenApiSecuritySchemeReference securitySchemeReference = new("ApiKey");
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
    app.UseSwaggerUI(options =>
    {
        string? configuredApiKey = app.Configuration["ApiSecurity:ApiKey"];
        if (!swaggerAutoAuthorize || string.IsNullOrWhiteSpace(configuredApiKey))
        {
            return;
        }

        // 用意：提供驗收/示範時的零設定體驗，開啟 Swagger 後即可直接呼叫受保護 API。
        // 風險：會把 API Key 暴露在瀏覽器端，因此僅建議內網或受控環境使用。
        string escapedHeaderName = apiSecurityHeaderName.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal);
        string escapedApiKey = configuredApiKey.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal);
        string jsHeaderName = $"'{escapedHeaderName}'";
        string jsApiKey = $"'{escapedApiKey}'";
        string serializedApiKey = JsonSerializer.Serialize(configuredApiKey);
        // 用意：即使使用者未點擊 Authorize，也會在每次送出前自動補上 API Key header。
        options.UseRequestInterceptor($"(request) => {{ request.headers = request.headers || {{}}; if (!request.headers[{jsHeaderName}]) {{ request.headers[{jsHeaderName}] = {jsApiKey}; }} return request; }}");
        options.EnablePersistAuthorization();
        // 用意：讓 Swagger UI 啟動時先完成 ApiKey 授權，減少驗收流程中的手動操作。
        options.HeadContent += $"<script>window.addEventListener('load',function(){{if(window.ui){{window.ui.preauthorizeApiKey('ApiKey',{serializedApiKey});}}}});</script>";
    });
}

app.UseCors(DependencyRegister.GetCorsPolicyName());
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
