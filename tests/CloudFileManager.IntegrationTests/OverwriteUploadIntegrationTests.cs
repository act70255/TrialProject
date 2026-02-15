using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Interfaces;
using CloudFileManager.Application.Models;
using CloudFileManager.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CloudFileManager.IntegrationTests;

public sealed class OverwriteUploadIntegrationTests
{
    [Fact]
    public void Should_OverwritePhysicalFileContent_WhenConflictPolicyIsOverwrite()
    {
        string basePath = Path.Combine(Path.GetTempPath(), $"cfm-overwrite-{Guid.NewGuid():N}");
        Directory.CreateDirectory(basePath);

        AppConfig config = ConfigDefaults.ApplyDefaults(new AppConfig
        {
            Storage = new StorageConfig
            {
                StorageRootPath = Path.Combine(basePath, "storage")
            },
            Database = new DatabaseConfig
            {
                Provider = "Sqlite",
                MigrateOnStartup = true,
                ConnectionStrings = new DatabaseConnectionStringsConfig
                {
                    Sqlite = $"Data Source={Path.Combine(basePath, "cloud-file-manager.db")}"
                }
            }
        });
        config.Management.FileConflictPolicy = "Overwrite";

        ServiceCollection services = new();
        services.AddSingleton(config);
        DependencyRegister.Register(services, config, basePath);
        CloudFileManager.Application.DependencyRegister.Register(services, config, basePath);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DependencyRegister.Initialize(serviceProvider, shouldMigrate: true);

        using IServiceScope scope = serviceProvider.CreateScope();
        ICloudFileApplicationService service = scope.ServiceProvider.GetRequiredService<ICloudFileApplicationService>();

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Docs")).Success);

        string sourceV1 = Path.Combine(basePath, "v1.txt");
        string sourceV2 = Path.Combine(basePath, "v2.txt");
        byte[] payloadV1 = "version-one"u8.ToArray();
        byte[] payloadV2 = "version-two-updated"u8.ToArray();
        File.WriteAllBytes(sourceV1, payloadV1);
        File.WriteAllBytes(sourceV2, payloadV2);

        OperationResult firstUpload = service.UploadFile(new UploadFileRequest(
            "Root/Docs",
            "dup.txt",
            payloadV1.Length,
            Encoding: "UTF-8",
            SourceLocalPath: sourceV1));

        OperationResult secondUpload = service.UploadFile(new UploadFileRequest(
            "Root/Docs",
            "dup.txt",
            payloadV2.Length,
            Encoding: "UTF-8",
            SourceLocalPath: sourceV2));

        Assert.True(firstUpload.Success);
        Assert.True(secondUpload.Success);

        string downloadedPath = Path.Combine(basePath, "download", "dup.txt");
        OperationResult downloadResult = service.DownloadFile(new DownloadFileRequest("Root/Docs/dup.txt", downloadedPath));

        Assert.True(downloadResult.Success);
        Assert.Equal(payloadV2, File.ReadAllBytes(downloadedPath));

        DirectoryTreeResult tree = service.GetDirectoryTree();
        int duplicateCount = tree.Lines.Count(line => line.Contains("dup.txt", StringComparison.Ordinal));
        Assert.Equal(1, duplicateCount);
    }
}
