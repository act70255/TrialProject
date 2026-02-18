using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Interfaces;
using CloudFileManager.Application.Models;
using CloudFileManager.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CloudFileManager.IntegrationTests;

public sealed class TagPersistenceIntegrationTests
{
    [Fact]
    public void Should_PersistTagsAcrossServiceProviders_AndSupportFindAndList()
    {
        string basePath = Path.Combine(Path.GetTempPath(), $"cfm-tag-persist-{Guid.NewGuid():N}");
        Directory.CreateDirectory(basePath);

        string docsPath = "Root/Docs";
        string filePath = "Root/Docs/spec.txt";

        using (ServiceProvider writerProvider = BuildServiceProvider(basePath))
        {
            using IServiceScope scope = writerProvider.CreateScope();
            ICloudFileApplicationService service = scope.ServiceProvider.GetRequiredService<ICloudFileApplicationService>();

            Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Docs")).Success);

            string sourcePath = Path.Combine(basePath, "spec.txt");
            File.WriteAllBytes(sourcePath, "spec-content"u8.ToArray());
            Assert.True(service.UploadFile(new UploadFileRequest(docsPath, "spec.txt", new FileInfo(sourcePath).Length, Encoding: "UTF-8", SourceLocalPath: sourcePath)).Success);

            Assert.True(service.AssignTag(new AssignTagRequest(docsPath, "Work")).Success);
            Assert.True(service.AssignTag(new AssignTagRequest(filePath, "Urgent")).Success);
        }

        using ServiceProvider readerProvider = BuildServiceProvider(basePath);
        using IServiceScope readerScope = readerProvider.CreateScope();
        ICloudFileApplicationService readerService = readerScope.ServiceProvider.GetRequiredService<ICloudFileApplicationService>();

        TagListResult allTags = readerService.ListTags(new ListTagsRequest());
        TaggedNodeResult docsNode = Assert.Single(allTags.Items, item => item.Path == docsPath);
        TaggedNodeResult fileNode = Assert.Single(allTags.Items, item => item.Path == filePath);
        Assert.Contains(docsNode.Tags, tag => tag.Name == "Work" && tag.Color == "Blue");
        Assert.Contains(fileNode.Tags, tag => tag.Name == "Urgent" && tag.Color == "Red");

        TagFindResult workInRoot = readerService.FindTags(new FindTagsRequest("work", "Root"));
        Assert.Equal("Work", workInRoot.Tag);
        Assert.Equal("Blue", workInRoot.Color);
        Assert.Contains(docsPath, workInRoot.Paths);
    }

    [Fact]
    public void Should_KeepTagBindingWhenFileIsRenamedOrMoved_AndRemoveOnDelete()
    {
        string basePath = Path.Combine(Path.GetTempPath(), $"cfm-tag-lifecycle-{Guid.NewGuid():N}");
        Directory.CreateDirectory(basePath);

        using ServiceProvider provider = BuildServiceProvider(basePath);
        using IServiceScope scope = provider.CreateScope();
        ICloudFileApplicationService service = scope.ServiceProvider.GetRequiredService<ICloudFileApplicationService>();

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Docs")).Success);
        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "Archive")).Success);

        string sourcePath = Path.Combine(basePath, "plan.txt");
        File.WriteAllBytes(sourcePath, "plan-v1"u8.ToArray());
        Assert.True(service.UploadFile(new UploadFileRequest("Root/Docs", "plan.txt", new FileInfo(sourcePath).Length, Encoding: "UTF-8", SourceLocalPath: sourcePath)).Success);

        Assert.True(service.AssignTag(new AssignTagRequest("Root/Docs/plan.txt", "Personal")).Success);

        Assert.True(service.RenameFile(new RenameFileRequest("Root/Docs/plan.txt", "plan-final.txt")).Success);
        Assert.True(service.MoveFile(new MoveFileRequest("Root/Docs/plan-final.txt", "Root/Archive")).Success);

        TagListResult movedTagResult = service.ListTags(new ListTagsRequest());
        Assert.DoesNotContain(movedTagResult.Items, item => item.Path == "Root/Docs/plan.txt");
        TaggedNodeResult movedNode = Assert.Single(movedTagResult.Items, item => item.Path == "Root/Archive/plan-final.txt");
        Assert.Contains(movedNode.Tags, tag => tag.Name == "Personal");

        Assert.True(service.DeleteFile(new DeleteFileRequest("Root/Archive/plan-final.txt")).Success);

        TagListResult afterDelete = service.ListTags(new ListTagsRequest("Root/Archive/plan-final.txt"));
        Assert.Empty(afterDelete.Items);
    }

    private static ServiceProvider BuildServiceProvider(string basePath)
    {
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

        ServiceCollection services = new();
        services.AddSingleton(config);
        DependencyRegister.Register(services, config, basePath);
        CloudFileManager.Application.DependencyRegister.Register(services, config, basePath);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        DependencyRegister.Initialize(serviceProvider, shouldMigrate: true);
        return serviceProvider;
    }
}
