using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Implementations;
using CloudFileManager.Application.Interfaces;
using CloudFileManager.Application.Models;
using CloudFileManager.Domain;
using CloudFileManager.Infrastructure.Configuration;
using CloudFileManager.Infrastructure.FileStorage;
using CloudFileManager.Infrastructure.DataAccess.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace CloudFileManager.UnitTests;

/// <summary>
/// PersistenceConsistencyTests 類別，負責封裝該領域的核心資料與行為。
/// </summary>
public class PersistenceConsistencyTests
{
    [Fact]
    public void Should_PersistFileAndMetadata_WhenUploadSucceeds()
    {
        var context = CreateStorageContextWithGateway(out AppConfig config, out string basePath);
        ICloudFileApplicationService service = CreateServiceWithGateway(config, basePath, context);

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Project_Docs")).Success);
        var upload = service.UploadFile(new UploadFileRequest("Root/Project_Docs", "Requirement.docx", 2048, PageCount: 2));

        Assert.True(upload.Success);

        var file = context.Files.Include(item => item.Metadata).Single(item => item.Name == "Requirement.docx");
        Assert.Equal(2048, file.SizeBytes);
        Assert.NotNull(file.Metadata);
        Assert.True(File.Exists(ResolveStoragePath(config, file.RelativePath)));
    }

    [Fact]
    public void Should_CopySourceLocalFileContent_WhenUploadUsesLocalPath()
    {
        var context = CreateStorageContextWithGateway(out AppConfig config, out string basePath);
        ICloudFileApplicationService service = CreateServiceWithGateway(config, basePath, context);

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Project_Docs")).Success);

        string sourcePath = Path.Combine(basePath, "source.txt");
        byte[] payload = "hello-cloud-file-manager"u8.ToArray();
        File.WriteAllBytes(sourcePath, payload);

        OperationResult upload = service.UploadFile(new UploadFileRequest(
            "Root/Project_Docs",
            "source.txt",
            payload.Length,
            Encoding: "UTF-8",
            SourceLocalPath: sourcePath));

        Assert.True(upload.Success);

        var file = context.Files.Single(item => item.Name == "source.txt");
        string storedFilePath = ResolveStoragePath(config, file.RelativePath);
        Assert.True(File.Exists(storedFilePath));
        Assert.Equal(payload, File.ReadAllBytes(storedFilePath));
    }

    [Fact]
    public void Should_SyncStorageAndMetadata_WhenMoveRenameDelete()
    {
        var context = CreateStorageContextWithGateway(out AppConfig config, out string basePath);
        ICloudFileApplicationService service = CreateServiceWithGateway(config, basePath, context);

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Project_Docs")).Success);
        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Archive")).Success);
        Assert.True(service.UploadFile(new UploadFileRequest("Root/Project_Docs", "temp.txt", 128, Encoding: "UTF-8")).Success);

        Assert.True(service.MoveFile(new MoveFileRequest("Root/Project_Docs/temp.txt", "Root/Archive")).Success);
        var moved = context.Files.Single(item => item.Name == "temp.txt");
        Assert.Contains("Archive", moved.RelativePath, StringComparison.Ordinal);
        Assert.True(File.Exists(ResolveStoragePath(config, moved.RelativePath)));

        Assert.True(service.RenameFile(new RenameFileRequest("Root/Archive/temp.txt", "final.txt")).Success);
        var renamed = context.Files.Single(item => item.Id == moved.Id);
        Assert.Equal("final.txt", renamed.Name);
        Assert.True(File.Exists(ResolveStoragePath(config, renamed.RelativePath)));

        Assert.True(service.DeleteFile(new DeleteFileRequest("Root/Archive/final.txt")).Success);
        Assert.False(context.Files.Any(item => item.Id == moved.Id));
        Assert.False(context.FileMetadata.Any(item => item.FileId == moved.Id));
        Assert.False(File.Exists(ResolveStoragePath(config, renamed.RelativePath)));
    }

    [Fact]
    public void Should_BootstrapProductionPath_WithoutInjectedSeedData()
    {
        string basePath = Path.Combine(Path.GetTempPath(), $"cfm-j5-{Guid.NewGuid():N}");
        Directory.CreateDirectory(basePath);

        string appsettings = """
        {
          "Storage": { "StorageRootPath": "./storage" },
          "Database": {
            "Provider": "Sqlite",
            "MigrateOnStartup": false,
            "ConnectionStrings": { "Sqlite": "Data Source=./data/cloud-file-manager.db" }
          }
        }
        """;
        File.WriteAllText(Path.Combine(basePath, "appsettings.json"), appsettings);

        AppConfig config = AppConfigLoader.Load(Path.Combine(basePath, "appsettings.json"));
        StorageBootstrapper.EnsureStorageRoot(config, basePath);

        ServiceCollection services = new();
        services.AddSingleton(config);
        CloudFileManager.Infrastructure.DependencyRegister.Register(services, config, basePath);
        CloudFileManager.Application.DependencyRegister.Register(services, config, basePath);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        CloudFileManager.Infrastructure.DependencyRegister.Initialize(serviceProvider, shouldMigrate: true);

        using IServiceScope scope = serviceProvider.CreateScope();
        ICloudFileApplicationService service = scope.ServiceProvider.GetRequiredService<ICloudFileApplicationService>();
        var tree = service.GetDirectoryTree();

        Assert.Single(tree.Lines);
        Assert.Contains("Root [目錄]", tree.Lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Should_CopyFileToTargetPath_WhenDownload()
    {
        var context = CreateStorageContextWithGateway(out AppConfig config, out string basePath);
        ICloudFileApplicationService service = CreateServiceWithGateway(config, basePath, context);

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Docs")).Success);
        Assert.True(service.UploadFile(new UploadFileRequest("Root/Docs", "download.txt", 256, Encoding: "UTF-8")).Success);

        string targetPath = Path.Combine(basePath, "download", "download.txt");
        OperationResult result = service.DownloadFile(new DownloadFileRequest("Root/Docs/download.txt", targetPath));

        Assert.True(result.Success);
        Assert.True(File.Exists(targetPath));
    }

    [Fact]
    public void Should_ApplyDirectoryPolicies_ForDeleteMoveRename()
    {
        var context = CreateStorageContextWithGateway(out AppConfig config, out string basePath);
        config.Management.DirectoryDeletePolicy = "ForbidNonEmpty";
        ICloudFileApplicationService service = CreateServiceWithGateway(config, basePath, context);

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "A")).Success);
        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root/A", "B")).Success);
        Assert.True(service.UploadFile(new UploadFileRequest("Root/A/B", "a.txt", 100, Encoding: "UTF-8")).Success);

        OperationResult forbidDelete = service.DeleteDirectory(new DeleteDirectoryRequest("Root/A"));
        Assert.False(forbidDelete.Success);

        OperationResult moveResult = service.MoveDirectory(new MoveDirectoryRequest("Root/A/B", "Root"));
        Assert.True(moveResult.Success);

        OperationResult renameResult = service.RenameDirectory(new RenameDirectoryRequest("Root/B", "B-Renamed"));
        Assert.True(renameResult.Success);

        config.Management.DirectoryDeletePolicy = "RecursiveDelete";
        OperationResult recursiveDelete = service.DeleteDirectory(new DeleteDirectoryRequest("Root/B-Renamed"));
        Assert.True(recursiveDelete.Success);
    }

    [Fact]
    public void Should_ApplyConflictPolicyUploadLimitAndAuditLog()
    {
        var context = CreateStorageContextWithGateway(out AppConfig config, out string basePath);
        config.Management.FileConflictPolicy = "Rename";
        config.Management.MaxUploadSizeBytes = 200;
        config.Management.EnableAuditLog = true;

        ICloudFileApplicationService service = CreateServiceWithGateway(config, basePath, context);
        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Docs")).Success);

        Assert.True(service.UploadFile(new UploadFileRequest("Root/Docs", "dup.txt", 100, Encoding: "UTF-8")).Success);
        Assert.True(service.UploadFile(new UploadFileRequest("Root/Docs", "dup.txt", 100, Encoding: "UTF-8")).Success);

        OperationResult overLimit = service.UploadFile(new UploadFileRequest("Root/Docs", "big.txt", 500, Encoding: "UTF-8"));
        Assert.False(overLimit.Success);

        int count = context.Files.Count(item => item.DirectoryId == context.Directories.Single(dir => dir.Name == "Docs").Id);
        Assert.Equal(2, count);

        string auditPath = Path.Combine(config.Storage.StorageRootPath, "audit.log");
        Assert.True(File.Exists(auditPath));
        string content = File.ReadAllText(auditPath);
        Assert.Contains("UPLOAD", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_RejectDuplicateUpload_WhenPolicyIsReject()
    {
        var context = CreateStorageContextWithGateway(out AppConfig config, out string basePath);
        config.Management.FileConflictPolicy = "Reject";
        ICloudFileApplicationService service = CreateServiceWithGateway(config, basePath, context);

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Docs")).Success);
        Assert.True(service.UploadFile(new UploadFileRequest("Root/Docs", "dup.txt", 100, Encoding: "UTF-8")).Success);

        OperationResult duplicate = service.UploadFile(new UploadFileRequest("Root/Docs", "dup.txt", 100, Encoding: "UTF-8"));

        Assert.False(duplicate.Success);
        Assert.Contains("already exists", duplicate.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Should_OverwriteDuplicateUpload_WhenPolicyIsOverwrite()
    {
        var context = CreateStorageContextWithGateway(out AppConfig config, out string basePath);
        config.Management.FileConflictPolicy = "Overwrite";
        ICloudFileApplicationService service = CreateServiceWithGateway(config, basePath, context);

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Docs")).Success);
        Assert.True(service.UploadFile(new UploadFileRequest("Root/Docs", "dup.txt", 100, Encoding: "UTF-8")).Success);

        OperationResult overwrite = service.UploadFile(new UploadFileRequest("Root/Docs", "dup.txt", 100, Encoding: "UTF-8"));

        Assert.True(overwrite.Success);
        int count = context.Files.Count(item => item.DirectoryId == context.Directories.Single(dir => dir.Name == "Docs").Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public void Should_KeepMemoryTreeConsistent_WhenOverwriteSaveFails()
    {
        SaveFailureInterceptor interceptor = new();
        var context = CreateStorageContextWithGateway(out AppConfig config, out string basePath, interceptor);
        config.Management.FileConflictPolicy = "Overwrite";
        ICloudFileApplicationService service = CreateServiceWithGateway(config, basePath, context);

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Docs")).Success);
        Assert.True(service.UploadFile(new UploadFileRequest("Root/Docs", "dup.txt", 100, Encoding: "UTF-8")).Success);

        interceptor.FailNextSave = true;
        OperationResult overwrite = service.UploadFile(new UploadFileRequest("Root/Docs", "dup.txt", 100, Encoding: "UTF-8"));

        Assert.False(overwrite.Success);

        DirectoryTreeResult tree = service.GetDirectoryTree();
        int duplicateCount = tree.Lines.Count(line => line.Contains("dup.txt", StringComparison.Ordinal));
        Assert.Equal(1, duplicateCount);

        int persistedCount = context.Files.Count(item => item.DirectoryId == context.Directories.Single(dir => dir.Name == "Docs").Id);
        Assert.Equal(1, persistedCount);
    }

    [Fact]
    public void Should_RollbackPhysicalMove_WhenDatabaseSaveFails()
    {
        SaveFailureInterceptor interceptor = new();
        var context = CreateStorageContextWithGateway(out AppConfig config, out string basePath, interceptor);
        ICloudFileApplicationService service = CreateServiceWithGateway(config, basePath, context);

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Project_Docs")).Success);
        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Archive")).Success);
        Assert.True(service.UploadFile(new UploadFileRequest("Root/Project_Docs", "temp.txt", 64, Encoding: "UTF-8")).Success);

        var sourceDirectory = context.Directories.Single(item => item.Name == "Project_Docs");
        var targetDirectory = context.Directories.Single(item => item.Name == "Archive");
        var fileEntity = context.Files.Single(item => item.Name == "temp.txt");
        string sourcePath = ResolveStoragePath(config, fileEntity.RelativePath);
        string targetPath = Path.Combine(ResolveStoragePath(config, targetDirectory.RelativePath), "temp.txt");

        interceptor.FailNextSave = true;
        OperationResult moveResult = service.MoveFile(new MoveFileRequest("Root/Project_Docs/temp.txt", "Root/Archive"));

        Assert.False(moveResult.Success);
        Assert.Contains("rolled back", moveResult.Message, StringComparison.OrdinalIgnoreCase);

        var reloaded = context.Files.Single(item => item.Id == fileEntity.Id);
        Assert.Equal(sourceDirectory.Id, reloaded.DirectoryId);
        Assert.Equal(fileEntity.RelativePath, reloaded.RelativePath);
        Assert.True(File.Exists(sourcePath));
        Assert.False(File.Exists(targetPath));
    }

    [Fact]
    public void Should_RollbackPhysicalRename_WhenDatabaseSaveFails()
    {
        SaveFailureInterceptor interceptor = new();
        var context = CreateStorageContextWithGateway(out AppConfig config, out string basePath, interceptor);
        ICloudFileApplicationService service = CreateServiceWithGateway(config, basePath, context);

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Project_Docs")).Success);
        Assert.True(service.UploadFile(new UploadFileRequest("Root/Project_Docs", "temp.txt", 64, Encoding: "UTF-8")).Success);

        var fileEntity = context.Files.Single(item => item.Name == "temp.txt");
        string originalPath = ResolveStoragePath(config, fileEntity.RelativePath);
        string renamedPath = Path.Combine(Path.GetDirectoryName(originalPath) ?? string.Empty, "final.txt");

        interceptor.FailNextSave = true;
        OperationResult renameResult = service.RenameFile(new RenameFileRequest("Root/Project_Docs/temp.txt", "final.txt"));

        Assert.False(renameResult.Success);
        Assert.Contains("rolled back", renameResult.Message, StringComparison.OrdinalIgnoreCase);

        var reloaded = context.Files.Single(item => item.Id == fileEntity.Id);
        Assert.Equal("temp.txt", reloaded.Name);
        Assert.Equal(fileEntity.RelativePath, reloaded.RelativePath);
        Assert.True(File.Exists(originalPath));
        Assert.False(File.Exists(renamedPath));
    }

    [Fact]
    public void Should_RollbackDirectoryMove_WhenDatabaseSaveFails()
    {
        SaveFailureInterceptor interceptor = new();
        var context = CreateStorageContextWithGateway(out AppConfig config, out string basePath, interceptor);
        ICloudFileApplicationService service = CreateServiceWithGateway(config, basePath, context);

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "A")).Success);
        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "B")).Success);

        var directoryA = context.Directories.Single(item => item.Name == "A");
        var rootDirectory = context.Directories.Single(item => item.ParentId == null && item.Name == "Root");
        var directoryB = context.Directories.Single(item => item.Name == "B");
        string sourcePath = ResolveStoragePath(config, directoryA.RelativePath);
        string targetPath = Path.Combine(ResolveStoragePath(config, directoryB.RelativePath), "A");

        interceptor.FailNextSave = true;
        OperationResult moveResult = service.MoveDirectory(new MoveDirectoryRequest("Root/A", "Root/B"));

        Assert.False(moveResult.Success);
        Assert.Contains("rolled back", moveResult.Message, StringComparison.OrdinalIgnoreCase);

        var reloaded = context.Directories.Single(item => item.Id == directoryA.Id);
        Assert.Equal(rootDirectory.Id, reloaded.ParentId);
        Assert.Equal(directoryA.RelativePath, reloaded.RelativePath);
        Assert.True(Directory.Exists(sourcePath));
        Assert.False(Directory.Exists(targetPath));
    }

    [Fact]
    public void Should_RollbackDirectoryRename_WhenDatabaseSaveFails()
    {
        SaveFailureInterceptor interceptor = new();
        var context = CreateStorageContextWithGateway(out AppConfig config, out string basePath, interceptor);
        ICloudFileApplicationService service = CreateServiceWithGateway(config, basePath, context);

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "A")).Success);

        var directoryA = context.Directories.Single(item => item.Name == "A");
        string originalPath = ResolveStoragePath(config, directoryA.RelativePath);
        string renamedPath = Path.Combine(Directory.GetParent(originalPath)?.FullName ?? string.Empty, "A-Renamed");

        interceptor.FailNextSave = true;
        OperationResult renameResult = service.RenameDirectory(new RenameDirectoryRequest("Root/A", "A-Renamed"));

        Assert.False(renameResult.Success);
        Assert.Contains("rolled back", renameResult.Message, StringComparison.OrdinalIgnoreCase);

        var reloaded = context.Directories.Single(item => item.Id == directoryA.Id);
        Assert.Equal("A", reloaded.Name);
        Assert.Equal(directoryA.RelativePath, reloaded.RelativePath);
        Assert.True(Directory.Exists(originalPath));
        Assert.False(Directory.Exists(renamedPath));
    }

    private static CloudFileDbContext CreateStorageContextWithGateway(out AppConfig config, out string basePath, SaveFailureInterceptor? interceptor = null)
    {
        basePath = Path.Combine(Path.GetTempPath(), $"cfm-j34-{Guid.NewGuid():N}");
        Directory.CreateDirectory(basePath);

        string databasePath = Path.Combine(basePath, "cloud-file-manager.db");
        config = ConfigDefaults.ApplyDefaults(new AppConfig
        {
            Storage = new StorageConfig { StorageRootPath = Path.Combine(basePath, "storage") },
            Database = new DatabaseConfig
            {
                Provider = "Sqlite",
                ConnectionStrings = new DatabaseConnectionStringsConfig
                {
                    Sqlite = $"Data Source={databasePath}"
                }
            }
        });

        DbContextOptionsBuilder<CloudFileDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlite(config.Database.ConnectionStrings.Sqlite);
        if (interceptor is not null)
        {
            optionsBuilder.AddInterceptors(interceptor);
        }

        CloudFileDbContext context = new(optionsBuilder.Options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        StorageMetadataInitializer.InitializeAsync(context, config.Storage.StorageRootPath).GetAwaiter().GetResult();
        return context;
    }

    private static ICloudFileApplicationService CreateServiceWithGateway(AppConfig config, string basePath, CloudFileDbContext context)
    {
        CloudFileFactoryRegistry registry = new([
            new WordFileFactory(),
            new ImageFileFactory(),
            new TextFileFactory()
        ]);

        StorageMetadataGateway gateway = new(context, config);
        CloudDirectory root = gateway.LoadRootTree();
        CloudFileReadModelService readModelService = new(root, config, basePath, new FileSystemXmlOutputWriter());
        CloudFileFileCommandService fileCommandService = new(root, registry, gateway, config);
        CloudFileDirectoryCommandService directoryCommandService = new(root, gateway, config);
        return new CloudFileApplicationService(readModelService, fileCommandService, directoryCommandService);
    }

    private static string ResolveStoragePath(AppConfig config, string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return config.Storage.StorageRootPath;
        }

        if (Path.IsPathRooted(storedPath))
        {
            return storedPath;
        }

        string normalized = storedPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(config.Storage.StorageRootPath, normalized));
    }

    private sealed class SaveFailureInterceptor : SaveChangesInterceptor
    {
        public bool FailNextSave { get; set; }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (!FailNextSave)
            {
                return result;
            }

            FailNextSave = false;
            throw new InvalidOperationException("Simulated SaveChanges failure.");
        }
    }
}
