using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Implementations;
using CloudFileManager.Application.Interfaces;
using CloudFileManager.Application.Models;
using CloudFileManager.Domain;
using CloudFileManager.Infrastructure.FileStorage;

namespace CloudFileManager.UnitTests;

/// <summary>
/// ConfigurationAcceptanceTests 類別，負責定義與承載設定資料。
/// </summary>
public class ConfigurationAcceptanceTests
{
    [Fact]
    public void Should_ResolveStoragePath_ForRelativeAndAbsoluteConfig()
    {
        string basePath = Path.Combine(Path.GetTempPath(), $"cfm-{Guid.NewGuid():N}");
        Directory.CreateDirectory(basePath);

        AppConfig relativeConfig = ConfigDefaults.ApplyDefaults(new AppConfig
        {
            Storage = new StorageConfig { StorageRootPath = "./storage-relative" }
        });

        string relative = StorageBootstrapper.EnsureStorageRoot(relativeConfig, basePath);
        string absoluteExpected = Path.Combine(basePath, "storage-absolute");

        AppConfig absoluteConfig = ConfigDefaults.ApplyDefaults(new AppConfig
        {
            Storage = new StorageConfig { StorageRootPath = absoluteExpected }
        });

        string absolute = StorageBootstrapper.EnsureStorageRoot(absoluteConfig, basePath);

        Assert.True(Directory.Exists(relative));
        Assert.True(Directory.Exists(absolute));
        Assert.Equal(absoluteExpected, absolute);
    }

    [Fact]
    public void Should_DisableTraverseLog_AndWriteXmlToFile_WhenConfigured()
    {
        string basePath = Path.Combine(Path.GetTempPath(), $"cfm-{Guid.NewGuid():N}");
        Directory.CreateDirectory(basePath);

        AppConfig config = ConfigDefaults.ApplyDefaults(new AppConfig
        {
            Logging = new LoggingConfig { Level = "Info", EnableTraverseLog = false },
            Output = new OutputConfig { XmlTarget = "File", XmlOutputPath = "./out/tree.xml" }
        });

        ICloudFileApplicationService service = CreateSeededService(config, basePath);

        var searchResult = service.SearchByExtension(new SearchByExtensionRequest(".docx"));
        var xmlResult = service.ExportXml();

        Assert.Empty(searchResult.TraverseLog);
        Assert.False(string.IsNullOrWhiteSpace(xmlResult.OutputPath));
        Assert.True(File.Exists(xmlResult.OutputPath!));
    }

    [Fact]
    public void Should_ApplyConfiguredExtensionGroups()
    {
        AppConfig config = ConfigDefaults.ApplyDefaults(new AppConfig
        {
            AllowedExtensions = new AllowedExtensionsConfig
            {
                Word = [".docx"],
                Image = [".png", ".jpg"],
                Text = [".txt"]
            }
        });

        ExtensionPolicy policy = new(config);

        Assert.True(policy.IsAllowedAny("DOCX"));
        Assert.True(policy.IsAllowedAny(".jpg"));
        Assert.True(policy.IsAllowedAny("txt"));
        Assert.False(policy.IsAllowedAny(".pdf"));
    }

    [Fact]
    public void Should_RejectUpload_WhenExtensionNotAllowed()
    {
        AppConfig config = ConfigDefaults.ApplyDefaults(new AppConfig
        {
            AllowedExtensions = new AllowedExtensionsConfig
            {
                Word = [".docx"],
                Image = [".png"],
                Text = [".txt"]
            }
        });

        ICloudFileApplicationService service = CreateSeededService(config, AppContext.BaseDirectory);
        OperationResult result = service.UploadFile(new UploadFileRequest("Root", "blocked.pdf", 128));

        Assert.False(result.Success);
        Assert.Contains("Unsupported file extension", result.Message);
    }

    [Fact]
    public void Should_ReportValidationErrors_ForInvalidVersionAndTraversalSettings()
    {
        AppConfig config = ConfigDefaults.ApplyDefaults(new AppConfig
        {
            Traversal = new TraversalConfig { Mode = "BFS", SiblingOrder = "ALPHA" }
        });
        config.ConfigVersion = "";

        IReadOnlyList<ConfigValidationError> errors = ConfigValidator.Validate(config);

        Assert.Contains(errors, item => item.ErrorCode == "CONF_VERSION_REQUIRED");
        Assert.Contains(errors, item => item.ErrorCode == "CONF_TRAVERSAL_MODE_INVALID");
        Assert.Contains(errors, item => item.ErrorCode == "CONF_SIBLING_ORDER_INVALID");
    }

    [Fact]
    public void Should_ExposeFeatureFlags_AndAllowFactoryUpload()
    {
        AppConfig config = ConfigDefaults.ApplyDefaults(new AppConfig
        {
            FeatureFlags = new FeatureFlagsConfig
            {
            }
        });

        ICloudFileApplicationService service = CreateSeededService(config, AppContext.BaseDirectory);
        var flags = service.GetFeatureFlags();

        Assert.Empty(flags.Flags);

        var uploadResult = service.UploadFile(new UploadFileRequest("Root", "design.docx", 1024, PageCount: 2));
        Assert.True(uploadResult.Success);
    }

    [Fact]
    public void Should_ReturnStandardErrorShape_WhenConfigValidationFails()
    {
        AppConfig config = ConfigDefaults.ApplyDefaults(new AppConfig
        {
            Logging = new LoggingConfig { Level = "Verbose" }
        });

        IReadOnlyList<ConfigValidationError> errors = ConfigValidator.Validate(config);

        ConfigValidationError error = Assert.Single(errors);
        Assert.False(string.IsNullOrWhiteSpace(error.ErrorCode));
        Assert.False(string.IsNullOrWhiteSpace(error.Field));
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public void Should_RejectInvalidDatabaseProvider()
    {
        AppConfig config = ConfigDefaults.ApplyDefaults(new AppConfig());
        config.Database.Provider = "Oracle";

        IReadOnlyList<ConfigValidationError> errors = ConfigValidator.Validate(config);

        Assert.Contains(errors, item => item.ErrorCode == "CONF_DB_PROVIDER_INVALID");
    }

    [Fact]
    public void Should_RequireConnectionString_ForSelectedDatabaseProvider()
    {
        AppConfig sqliteConfig = ConfigDefaults.ApplyDefaults(new AppConfig());
        sqliteConfig.Database.Provider = "Sqlite";
        sqliteConfig.Database.ConnectionStrings.Sqlite = "";

        AppConfig sqlServerConfig = ConfigDefaults.ApplyDefaults(new AppConfig());
        sqlServerConfig.Database.Provider = "SqlServer";
        sqlServerConfig.Database.ConnectionStrings.SqlServer = "";

        IReadOnlyList<ConfigValidationError> sqliteErrors = ConfigValidator.Validate(sqliteConfig);
        IReadOnlyList<ConfigValidationError> sqlServerErrors = ConfigValidator.Validate(sqlServerConfig);

        Assert.Contains(sqliteErrors, item => item.Field == "Database.ConnectionStrings.Sqlite");
        Assert.Contains(sqlServerErrors, item => item.Field == "Database.ConnectionStrings.SqlServer");
    }

    [Fact]
    public void Should_DefaultDatabaseConfig_ToSqliteWithoutAutoMigration()
    {
        AppConfig config = ConfigDefaults.ApplyDefaults(new AppConfig());

        Assert.Equal("Sqlite", config.Database.Provider);
        Assert.False(config.Database.MigrateOnStartup);
    }

    private static ICloudFileApplicationService CreateSeededService(AppConfig config, string basePath)
    {
        CloudFileFactoryRegistry registry = new([
            new WordFileFactory(),
            new ImageFileFactory(),
            new TextFileFactory()
        ]);
        CloudDirectory root = new("Root", DateTime.UtcNow);
        CloudFileReadModelService readModelService = new(root, config, basePath, new FileSystemXmlOutputWriter());
        CloudFileFileCommandService fileCommandService = new(root, registry, NoOpStorageMetadataGateway.Instance, config);
        CloudFileDirectoryCommandService directoryCommandService = new(root, NoOpStorageMetadataGateway.Instance, config);

        ICloudFileApplicationService service = new CloudFileApplicationService(readModelService, fileCommandService, directoryCommandService);
        service.CreateDirectory(new CreateDirectoryRequest("Root", "Project_Docs"));
        service.UploadFile(new UploadFileRequest("Root/Project_Docs", "Requirement.docx", 500 * 1024, PageCount: 120));
        return service;
    }
}
