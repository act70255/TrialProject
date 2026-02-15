using CloudFileManager.Presentation.Website.Services;

namespace CloudFileManager.Presentation.Website;

/// <summary>
/// DependencyRegister 類別，負責組態組裝與相依性註冊。
/// </summary>
public static class DependencyRegister
{
    /// <summary>
    /// 註冊資料。
    /// </summary>
    public static void Register(WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithViews();
        string webApiBaseUrl = builder.Configuration["WebApiBaseUrl"] ?? "http://localhost:5181/";
        string apiKeyHeaderName = builder.Configuration["ApiSecurity:HeaderName"] ?? "X-Api-Key";
        string apiKey = builder.Configuration["ApiSecurity:ApiKey"] ?? string.Empty;

        if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("ApiSecurity:ApiKey is required in non-development environments.");
        }

        builder.Services.AddHttpClient<FileSystemApiClient>(client =>
        {
            client.BaseAddress = new Uri(webApiBaseUrl);

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Add(apiKeyHeaderName, apiKey);
            }
        });
    }
}
