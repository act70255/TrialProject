namespace CloudFileManager.Application.Configuration;

/// <summary>
/// ConfigDefaults 類別，負責定義與承載設定資料。
/// </summary>
public static class ConfigDefaults
{
    /// <summary>
    /// 套用設定預設值。
    /// </summary>
    public static AppConfig ApplyDefaults(AppConfig? config)
    {
        AppConfig finalConfig = config ?? new AppConfig();
        finalConfig.Storage ??= new StorageConfig();
        finalConfig.Logging ??= new LoggingConfig();
        finalConfig.Output ??= new OutputConfig();
        finalConfig.AllowedExtensions ??= new AllowedExtensionsConfig();
        finalConfig.FeatureFlags ??= new FeatureFlagsConfig();
        finalConfig.Database ??= new DatabaseConfig();
        finalConfig.Database.ConnectionStrings ??= new DatabaseConnectionStringsConfig();
        finalConfig.Management ??= new ManagementConfig();

        if (string.IsNullOrWhiteSpace(finalConfig.ConfigVersion))
        {
            finalConfig.ConfigVersion = "1.0";
        }

        if (string.IsNullOrWhiteSpace(finalConfig.Logging.Level))
        {
            finalConfig.Logging.Level = "Info";
        }

        if (string.IsNullOrWhiteSpace(finalConfig.Output.XmlTarget))
        {
            finalConfig.Output.XmlTarget = "Console";
        }

        if (string.IsNullOrWhiteSpace(finalConfig.Output.XmlOutputPath))
        {
            finalConfig.Output.XmlOutputPath = "./output/tree.xml";
        }

        if (string.IsNullOrWhiteSpace(finalConfig.Database.Provider))
        {
            finalConfig.Database.Provider = "Sqlite";
        }

        if (string.IsNullOrWhiteSpace(finalConfig.Database.ConnectionStrings.Sqlite))
        {
            finalConfig.Database.ConnectionStrings.Sqlite = "Data Source=./data/cloud-file-manager.db";
        }

        if (string.IsNullOrWhiteSpace(finalConfig.Database.ConnectionStrings.SqlServer))
        {
            finalConfig.Database.ConnectionStrings.SqlServer = "Server=localhost;Database=CloudFileManager;User Id=sa;Password=Your_password123;TrustServerCertificate=true";
        }

        if (string.IsNullOrWhiteSpace(finalConfig.Management.FileConflictPolicy))
        {
            finalConfig.Management.FileConflictPolicy = "Reject";
        }

        if (string.IsNullOrWhiteSpace(finalConfig.Management.DirectoryDeletePolicy))
        {
            finalConfig.Management.DirectoryDeletePolicy = "ForbidNonEmpty";
        }

        if (finalConfig.Management.MaxUploadSizeBytes <= 0)
        {
            finalConfig.Management.MaxUploadSizeBytes = 10 * 1024 * 1024;
        }

        finalConfig.AllowedExtensions.Word = EnsureDefaults(finalConfig.AllowedExtensions.Word, [".docx"]);
        finalConfig.AllowedExtensions.Image = EnsureDefaults(finalConfig.AllowedExtensions.Image, [".png", ".jpg", ".jpeg"]);
        finalConfig.AllowedExtensions.Text = EnsureDefaults(finalConfig.AllowedExtensions.Text, [".txt"]);

        return finalConfig;
    }

    /// <summary>
    /// 正規化副檔名字串格式。
    /// </summary>
    public static string NormalizeExtension(string extension)
    {
        string normalized = extension.Trim().ToLowerInvariant();
        if (!normalized.StartsWith('.'))
        {
            normalized = $".{normalized}";
        }

        return normalized;
    }

    /// <summary>
    /// 補齊缺漏的預設設定值。
    /// </summary>
    private static List<string> EnsureDefaults(List<string>? source, List<string> defaults)
    {
        if (source is null || source.Count == 0)
        {
            return defaults;
        }

        return source
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(NormalizeExtension)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
