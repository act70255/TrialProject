using CloudFileManager.Application.Configuration;
using Microsoft.Extensions.Configuration;

namespace CloudFileManager.Infrastructure.Configuration;

/// <summary>
/// AppConfigLoader 類別，負責定義與承載設定資料。
/// </summary>
public static class AppConfigLoader
{
    /// <summary>
    /// 載入資料。
    /// </summary>
    public static AppConfig Load(string configFilePath)
    {
        string basePath = Path.GetDirectoryName(configFilePath) ?? AppContext.BaseDirectory;
        string configFileName = Path.GetFileName(configFilePath);

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(configFileName, optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        AppConfig? raw = configuration.Get<AppConfig>();

        AppConfig config = ConfigDefaults.ApplyDefaults(raw);
        IReadOnlyList<ConfigValidationError> errors = ConfigValidator.Validate(config);
        if (errors.Count > 0)
        {
            string detail = string.Join("; ", errors.Select(item => $"{item.ErrorCode}:{item.Field}"));
            throw new InvalidOperationException($"Invalid configuration: {detail}");
        }

        return config;
    }
}
