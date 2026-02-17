namespace CloudFileManager.Application.Configuration;

public sealed class AppConfig
{
    public string ConfigVersion { get; set; } = "1.0";

    public bool UseSwagger { get; set; } = true;

    public StorageConfig Storage { get; set; } = new();

    public LoggingConfig Logging { get; set; } = new();

    public OutputConfig Output { get; set; } = new();

    public AllowedExtensionsConfig AllowedExtensions { get; set; } = new();

    public FeatureFlagsConfig FeatureFlags { get; set; } = new();

    public DatabaseConfig Database { get; set; } = new();

    public ManagementConfig Management { get; set; } = new();
}

public sealed class StorageConfig
{
    public string StorageRootPath { get; set; } = "./storage";
}

public sealed class LoggingConfig
{
    public string Level { get; set; } = "Info";

    public bool EnableTraverseLog { get; set; } = true;
}

public sealed class OutputConfig
{
    public string XmlTarget { get; set; } = "Console";

    public string XmlOutputPath { get; set; } = "./output/tree.xml";
}

public sealed class AllowedExtensionsConfig
{
    public List<string> Word { get; set; } = new();

    public List<string> Image { get; set; } = new();

    public List<string> Text { get; set; } = new();
}

public sealed class FeatureFlagsConfig
{
}

public sealed class DatabaseConfig
{
    public string Provider { get; set; } = "Sqlite";

    public bool MigrateOnStartup { get; set; }

    public DatabaseConnectionStringsConfig ConnectionStrings { get; set; } = new();
}

public sealed class DatabaseConnectionStringsConfig
{
    public string Sqlite { get; set; } = "Data Source=./data/cloud-file-manager.db";

    public string SqlServer { get; set; } = "Server=localhost;Database=CloudFileManager;User Id=sa;Password=Your_password123;TrustServerCertificate=true";
}

public sealed class ManagementConfig
{
    public string FileConflictPolicy { get; set; } = "Reject";

    public string DirectoryDeletePolicy { get; set; } = "ForbidNonEmpty";

    public long MaxUploadSizeBytes { get; set; } = 10 * 1024 * 1024;

    public bool EnableAuditLog { get; set; } = true;

    public FileConflictPolicyType GetFileConflictPolicy()
    {
        return ManagementPolicyParser.ParseFileConflictPolicy(FileConflictPolicy);
    }

    public DirectoryDeletePolicyType GetDirectoryDeletePolicy()
    {
        return ManagementPolicyParser.ParseDirectoryDeletePolicy(DirectoryDeletePolicy);
    }
}
