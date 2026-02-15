using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Implementations;
using CloudFileManager.Application.Models;
using CloudFileManager.Domain;

namespace CloudFileManager.UnitTests;

public sealed class BusinessLogicCommandServiceTests
{
    [Fact]
    public void DirectoryCommand_ShouldBlockRootOperations_AndDescendantMove()
    {
        AppConfig config = ConfigDefaults.ApplyDefaults(new AppConfig());
        CloudDirectory root = new("Root", DateTime.UtcNow);
        CloudFileDirectoryCommandService service = new(root, NoOpStorageMetadataGateway.Instance, config);

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "A")).Success);
        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root/A", "B")).Success);

        OperationResult deleteRoot = service.DeleteDirectory(new DeleteDirectoryRequest("Root"));
        OperationResult moveRoot = service.MoveDirectory(new MoveDirectoryRequest("Root", "Root/A"));
        OperationResult renameRoot = service.RenameDirectory(new RenameDirectoryRequest("Root", "Root2"));
        OperationResult moveToDescendant = service.MoveDirectory(new MoveDirectoryRequest("Root/A", "Root/A/B"));

        Assert.False(deleteRoot.Success);
        Assert.False(moveRoot.Success);
        Assert.False(renameRoot.Success);
        Assert.False(moveToDescendant.Success);
    }

    [Fact]
    public void FileCommand_ShouldValidateExtensionSizeAndGatewayRequirements()
    {
        AppConfig config = ConfigDefaults.ApplyDefaults(new AppConfig());
        config.Management.MaxUploadSizeBytes = 100;

        CloudDirectory root = new("Root", DateTime.UtcNow);
        root.AddDirectory("Docs", DateTime.UtcNow);

        CloudFileFactoryRegistry registry = new([
            new WordFileFactory(),
            new ImageFileFactory(),
            new TextFileFactory()
        ]);

        CloudFileFileCommandService service = new(root, registry, NoOpStorageMetadataGateway.Instance, config);

        OperationResult unsupportedExtension = service.UploadFile(new UploadFileRequest("Root/Docs", "note.exe", 10));
        OperationResult overLimit = service.UploadFile(new UploadFileRequest("Root/Docs", "note.txt", 101, Encoding: "UTF-8"));
        OperationResult uploaded = service.UploadFile(new UploadFileRequest("Root/Docs", "ok.txt", 80, Encoding: "UTF-8"));
        OperationResult downloadWithoutGateway = service.DownloadFile(new DownloadFileRequest("Root/Docs/ok.txt", "C:/tmp/ok.txt"));
        FileDownloadResult contentWithoutGateway = service.DownloadFileContent("Root/Docs/ok.txt");

        Assert.False(unsupportedExtension.Success);
        Assert.Contains("Unsupported file extension", unsupportedExtension.Message, StringComparison.Ordinal);

        Assert.False(overLimit.Success);
        Assert.Contains("exceeds limit", overLimit.Message, StringComparison.OrdinalIgnoreCase);

        Assert.True(uploaded.Success);

        Assert.False(downloadWithoutGateway.Success);
        Assert.Contains("requires storage gateway", downloadWithoutGateway.Message, StringComparison.OrdinalIgnoreCase);

        Assert.False(contentWithoutGateway.Success);
        Assert.Contains("requires storage gateway", contentWithoutGateway.Message, StringComparison.OrdinalIgnoreCase);
    }
}
