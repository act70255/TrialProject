using CloudFileManager.Contracts;

namespace CloudFileManager.UnitTests;

public sealed class ContractDtoTests
{
    [Fact]
    public void DefaultConstructors_ShouldInitializeExpectedValues()
    {
        Assert.Equal(string.Empty, new CreateDirectoryRequestDto().ParentPath);
        Assert.Equal(string.Empty, new CreateDirectoryRequestDto().DirectoryName);

        Assert.Equal(string.Empty, new UploadFileRequestDto().DirectoryPath);
        Assert.Equal(string.Empty, new UploadFileRequestDto().FileName);

        Assert.Equal(string.Empty, new MoveFileRequestDto().SourceFilePath);
        Assert.Equal(string.Empty, new MoveFileRequestDto().TargetDirectoryPath);

        Assert.Equal(string.Empty, new RenameFileRequestDto().FilePath);
        Assert.Equal(string.Empty, new RenameFileRequestDto().NewFileName);

        Assert.Equal(string.Empty, new DeleteFileRequestDto().FilePath);
        Assert.Equal(string.Empty, new DeleteDirectoryRequestDto().DirectoryPath);
        Assert.Equal(string.Empty, new MoveDirectoryRequestDto().SourceDirectoryPath);
        Assert.Equal(string.Empty, new MoveDirectoryRequestDto().TargetParentDirectoryPath);
        Assert.Equal(string.Empty, new RenameDirectoryRequestDto().DirectoryPath);
        Assert.Equal(string.Empty, new RenameDirectoryRequestDto().NewDirectoryName);

        Assert.Equal(string.Empty, new CalculateSizeRequestDto().DirectoryPath);
        Assert.Equal(string.Empty, new SearchByExtensionRequestDto().Extension);

        Assert.Empty(new DirectoryTreeResultDto().Lines);
        Assert.Empty(new SearchResultDto().Paths);
        Assert.Empty(new SearchResultDto().TraverseLog);

        Assert.False(new OperationResultDto().Success);
        Assert.Equal(string.Empty, new OperationResultDto().Message);
        Assert.Null(new OperationResultDto().ErrorCode);

        Assert.True(new SizeCalculationResultDto().IsFound);
        Assert.Equal(0, new SizeCalculationResultDto().SizeBytes);
        Assert.Equal(string.Empty, new SizeCalculationResultDto().FormattedSize);
        Assert.Empty(new SizeCalculationResultDto().TraverseLog);

        Assert.False(new FileDownloadResultDto().Success);
        Assert.Equal(string.Empty, new FileDownloadResultDto().Message);
        Assert.Equal(string.Empty, new FileDownloadResultDto().FileName);
        Assert.Equal("application/octet-stream", new FileDownloadResultDto().ContentType);

        Assert.Empty(new FeatureFlagsResultDto().Flags);

        Assert.Equal(string.Empty, new XmlExportResultDto().XmlContent);
        Assert.Null(new XmlExportResultDto().OutputPath);
    }

    [Fact]
    public void ParameterizedConstructors_ShouldMapInputValues()
    {
        var createDirectory = new CreateDirectoryRequestDto("Root", "Docs");
        Assert.Equal("Root", createDirectory.ParentPath);
        Assert.Equal("Docs", createDirectory.DirectoryName);

        var uploadFile = new UploadFileRequestDto("Root", "a.txt", 12, 3, 640, 480, "UTF-8", "C:/tmp/a.txt");
        Assert.Equal("Root", uploadFile.DirectoryPath);
        Assert.Equal("a.txt", uploadFile.FileName);
        Assert.Equal(12, uploadFile.Size);
        Assert.Equal(3, uploadFile.PageCount);
        Assert.Equal(640, uploadFile.Width);
        Assert.Equal(480, uploadFile.Height);
        Assert.Equal("UTF-8", uploadFile.Encoding);
        Assert.Equal("C:/tmp/a.txt", uploadFile.SourceLocalPath);

        var moveFile = new MoveFileRequestDto("Root/a.txt", "Root/B");
        Assert.Equal("Root/a.txt", moveFile.SourceFilePath);
        Assert.Equal("Root/B", moveFile.TargetDirectoryPath);

        var renameFile = new RenameFileRequestDto("Root/a.txt", "b.txt");
        Assert.Equal("Root/a.txt", renameFile.FilePath);
        Assert.Equal("b.txt", renameFile.NewFileName);

        var deleteFile = new DeleteFileRequestDto("Root/a.txt");
        Assert.Equal("Root/a.txt", deleteFile.FilePath);

        var deleteDirectory = new DeleteDirectoryRequestDto("Root/A");
        Assert.Equal("Root/A", deleteDirectory.DirectoryPath);

        var moveDirectory = new MoveDirectoryRequestDto("Root/A", "Root/B");
        Assert.Equal("Root/A", moveDirectory.SourceDirectoryPath);
        Assert.Equal("Root/B", moveDirectory.TargetParentDirectoryPath);

        var renameDirectory = new RenameDirectoryRequestDto("Root/A", "Archive");
        Assert.Equal("Root/A", renameDirectory.DirectoryPath);
        Assert.Equal("Archive", renameDirectory.NewDirectoryName);

        var calculateSize = new CalculateSizeRequestDto("Root/A");
        Assert.Equal("Root/A", calculateSize.DirectoryPath);

        var searchByExtension = new SearchByExtensionRequestDto(".txt");
        Assert.Equal(".txt", searchByExtension.Extension);

        var tree = new DirectoryTreeResultDto(["Root", "Root/A"]);
        Assert.Equal(2, tree.Lines.Count);

        var search = new SearchResultDto(["Root/A/a.txt"], ["trace"]);
        Assert.Single(search.Paths);
        Assert.Single(search.TraverseLog);

        var operation = new OperationResultDto(false, "failed", "E001");
        Assert.False(operation.Success);
        Assert.Equal("failed", operation.Message);
        Assert.Equal("E001", operation.ErrorCode);

        var operationWithoutCode = new OperationResultDto(true, "ok");
        Assert.True(operationWithoutCode.Success);
        Assert.Equal("ok", operationWithoutCode.Message);
        Assert.Null(operationWithoutCode.ErrorCode);

        var size = new SizeCalculationResultDto(false, 123, "123 Bytes", ["trace"]);
        Assert.False(size.IsFound);
        Assert.Equal(123, size.SizeBytes);
        Assert.Equal("123 Bytes", size.FormattedSize);
        Assert.Single(size.TraverseLog);

        var foundSize = new SizeCalculationResultDto(456, "456 Bytes", ["trace2"]);
        Assert.True(foundSize.IsFound);
        Assert.Equal(456, foundSize.SizeBytes);
        Assert.Equal("456 Bytes", foundSize.FormattedSize);
        Assert.Single(foundSize.TraverseLog);

        byte[] content = [1, 2, 3];
        var download = new FileDownloadResultDto(true, "ok", "a.txt", content, "text/plain");
        Assert.True(download.Success);
        Assert.Equal("ok", download.Message);
        Assert.Equal("a.txt", download.FileName);
        Assert.Equal(content, download.Content);
        Assert.Equal("text/plain", download.ContentType);

        var flags = new FeatureFlagsResultDto(new Dictionary<string, bool>
        {
            ["EnableWebsite"] = true
        });
        Assert.True(flags.Flags["EnableWebsite"]);

        var xml = new XmlExportResultDto("<root />", "C:/tmp/tree.xml");
        Assert.Equal("<root />", xml.XmlContent);
        Assert.Equal("C:/tmp/tree.xml", xml.OutputPath);
    }
}
