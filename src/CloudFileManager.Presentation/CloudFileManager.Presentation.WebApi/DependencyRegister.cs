using CloudFileManager.Application.Configuration;
using CloudFileManager.Infrastructure.Configuration;
using CloudFileManager.Presentation.WebApi.Services;
using CloudFileManager.Presentation.WebApi.Security;
using Microsoft.Extensions.DependencyInjection;

namespace CloudFileManager.Presentation.WebApi;

public static class DependencyRegister
{
    private const string ApiKeyScheme = "ApiKey";
    private const string CorsPolicyName = "WebsiteClient";

    public static bool RegisterServices(WebApplicationBuilder builder)
    {
        string configFilePath = ConfigPathResolver.ResolveConfigFilePath(builder.Environment.ContentRootPath, AppContext.BaseDirectory);
        string basePath = ConfigPathResolver.ResolveRuntimeBasePath(configFilePath, Directory.GetCurrentDirectory(), AppContext.BaseDirectory, "TrialProject.sln");
        AppConfig config = AppConfigLoader.Load(configFilePath);
        bool shouldMigrate = config.Database.MigrateOnStartup;
        string apiKey = builder.Configuration["ApiSecurity:ApiKey"] ?? string.Empty;

        if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("ApiSecurity:ApiKey is required in non-development environments.");
        }

        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5189", "https://localhost:7206", "http://localhost:5190"];
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        builder.Services
            .AddAuthentication(ApiKeyScheme)
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyScheme, options =>
            {
                options.HeaderName = builder.Configuration["ApiSecurity:HeaderName"] ?? "X-Api-Key";
                options.ApiKey = apiKey;
            });
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<WebApiSessionStateProcessor>();
        builder.Services.AddSingleton(config);
        CloudFileManager.Infrastructure.DependencyRegister.Register(builder.Services, config, basePath);
        CloudFileManager.Application.DependencyRegister.Register(builder.Services, config, basePath);

        return shouldMigrate;
    }

    public static void InitializeInfrastructure(WebApplication app, bool shouldMigrate)
    {
        CloudFileManager.Infrastructure.DependencyRegister.Initialize(app.Services, shouldMigrate);
    }

    public static Task InitializeInfrastructureAsync(WebApplication app, bool shouldMigrate, CancellationToken cancellationToken = default)
    {
        return CloudFileManager.Infrastructure.DependencyRegister.InitializeAsync(app.Services, shouldMigrate, cancellationToken);
    }

    public static string GetCorsPolicyName()
    {
        return CorsPolicyName;
    }
}
