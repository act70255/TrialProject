using CloudFileManager.Application.Implementations;
using CloudFileManager.Application.Interfaces;
using CloudFileManager.Application.Models;
using CloudFileManager.Application.Configuration;
using CloudFileManager.Domain;
using CloudFileManager.Infrastructure.FileStorage;

namespace CloudFileManager.UnitTests;

/// <summary>
/// MvpAcceptanceTests 類別，負責封裝該領域的核心資料與行為。
/// </summary>
public class MvpAcceptanceTests
{
    [Fact]
    public void Should_BuildDirectoryTree_WithSeedData()
    {
        ICloudFileApplicationService service = CreateSeededService();

        var result = service.GetDirectoryTree();

        Assert.Contains(result.Lines, item => item.Contains("Root [目錄]", StringComparison.Ordinal));
        Assert.Contains(result.Lines, item => item.Contains("Requirement.docx", StringComparison.Ordinal));
    }

    [Fact]
    public void Should_CalculateTotalSize_UsingBinaryUnits()
    {
        ICloudFileApplicationService service = CreateSeededService();

        var result = service.CalculateTotalSize(new CalculateSizeRequest("Root"));

        Assert.Equal((500 * 1024) + (2 * 1024 * 1024) + 1024 + (200 * 1024) + 500, result.SizeBytes);
        Assert.Equal("2749.488KB", result.FormattedSize);
    }

    [Fact]
    public void Should_SearchByExtension_InDfsPreOrder()
    {
        ICloudFileApplicationService service = CreateSeededService();

        var result = service.SearchByExtension(new SearchByExtensionRequest(".docx"));

        Assert.Equal("Root/Project_Docs/Requirement.docx", result.Paths[0]);
        Assert.Equal("Root/Personal_Notes/Archive_2025/Meeting-Archive.docx", result.Paths[1]);
    }

    [Fact]
    public void Should_ReturnDirectoryEntries_WithRawAndFormattedSize()
    {
        ICloudFileApplicationService service = CreateSeededService();

        DirectoryEntriesResult result = service.GetDirectoryEntries(new ListDirectoryEntriesRequest("Root"));

        Assert.True(result.IsFound);
        Assert.NotEmpty(result.Entries);
        Assert.All(result.Entries, entry =>
        {
            Assert.True(entry.SizeBytes >= 0);
            Assert.Equal(ByteSizeFormatter.Format(entry.SizeBytes), entry.FormattedSize);
        });
    }

    [Fact]
    public void Should_KeepSizeSortByRawBytes_AndDisplayFormattedSize()
    {
        ICloudFileApplicationService service = CreateSeededService();

        DirectoryEntriesResult result = service.GetDirectoryEntries(new ListDirectoryEntriesRequest("Root"));
        List<DirectoryEntryResult> sorted = result.Entries
            .OrderByDescending(item => item.SizeBytes)
            .ThenBy(item => item.SiblingOrder)
            .ToList();

        Assert.True(sorted.Count >= 2);
        Assert.True(sorted[0].SizeBytes >= sorted[1].SizeBytes);
        Assert.Equal(ByteSizeFormatter.Format(sorted[0].SizeBytes), sorted[0].FormattedSize);
        Assert.Equal(ByteSizeFormatter.Format(sorted[1].SizeBytes), sorted[1].FormattedSize);
    }

    [Fact]
    public void Should_ExportSemanticXml_ForDirectoryTree()
    {
        ICloudFileApplicationService service = CreateSeededService();

        var result = service.ExportXml();

        Assert.Contains("<Directory Name=\"Root\"", result.XmlContent, StringComparison.Ordinal);
        Assert.Contains("<File Name=\"Requirement.docx\"", result.XmlContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_ProvideTraverseLog_ForSearchAndSize()
    {
        ICloudFileApplicationService service = CreateSeededService();

        var sizeResult = service.CalculateTotalSize(new CalculateSizeRequest("Root"));
        var searchResult = service.SearchByExtension(new SearchByExtensionRequest(".txt"));

        Assert.NotEmpty(sizeResult.TraverseLog);
        Assert.NotEmpty(searchResult.TraverseLog);
    }

    [Fact]
    public void Should_AllowMoveDirectory_WhenTargetSharesPrefixButIsNotDescendant()
    {
        ICloudFileApplicationService service = CreateSeededService();

        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "A")).Success);
        Assert.True(service.CreateDirectory(new CreateDirectoryRequest("Root", "AB")).Success);
        Assert.True(service.UploadFile(new UploadFileRequest("Root/A", "note.txt", 100, Encoding: "UTF-8")).Success);

        OperationResult moveResult = service.MoveDirectory(new MoveDirectoryRequest("Root/A", "Root/AB"));

        Assert.True(moveResult.Success);
        SearchResult searchResult = service.SearchByExtension(new SearchByExtensionRequest(".txt"));
        Assert.Contains(searchResult.Paths, item => item.Equals("Root/AB/A/note.txt", StringComparison.Ordinal));
    }

    private static ICloudFileApplicationService CreateSeededService()
    {
        AppConfig config = ConfigDefaults.ApplyDefaults(new AppConfig());
        CloudFileFactoryRegistry registry = new([
            new WordFileFactory(),
            new ImageFileFactory(),
            new TextFileFactory()
        ]);
        CloudDirectory root = new("Root", DateTime.UtcNow);
        CloudFileReadModelService readModelService = new(root, config, AppContext.BaseDirectory, new FileSystemXmlOutputWriter());
        CloudFileFileCommandService fileCommandService = new(root, registry, NoOpStorageMetadataGateway.Instance, config);
        CloudFileDirectoryCommandService directoryCommandService = new(root, NoOpStorageMetadataGateway.Instance, config);

        ICloudFileApplicationService service = new CloudFileApplicationService(readModelService, fileCommandService, directoryCommandService, NoOpStorageMetadataGateway.Instance);

        service.CreateDirectory(new CreateDirectoryRequest("Root", "Project_Docs"));
        service.CreateDirectory(new CreateDirectoryRequest("Root", "Personal_Notes"));
        service.CreateDirectory(new CreateDirectoryRequest("Root/Personal_Notes", "Archive_2025"));

        service.UploadFile(new UploadFileRequest("Root/Project_Docs", "Requirement.docx", 500 * 1024, PageCount: 120));
        service.UploadFile(new UploadFileRequest("Root/Project_Docs", "Architecture.png", 2 * 1024 * 1024, Width: 2560, Height: 1440));
        service.UploadFile(new UploadFileRequest("Root/Personal_Notes", "Todo.txt", 1024, Encoding: "UTF-8"));
        service.UploadFile(new UploadFileRequest("Root/Personal_Notes/Archive_2025", "Meeting-Archive.docx", 200 * 1024, PageCount: 30));
        service.UploadFile(new UploadFileRequest("Root", "Readme.txt", 500, Encoding: "ASCII"));

        return service;
    }
}
