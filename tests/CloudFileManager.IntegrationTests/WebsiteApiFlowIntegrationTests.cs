extern alias website;

using CloudFileManager.Contracts;
using CloudFileManager.Presentation.WebApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using FileSystemApiClient = website::CloudFileManager.Presentation.Website.Services.FileSystemApiClient;

namespace CloudFileManager.IntegrationTests;

/// <summary>
/// WebsiteAPIFlowIntegrationTests 類別，負責封裝該領域的核心資料與行為。
/// </summary>
public sealed class WebsiteApiFlowIntegrationTests
{
    [Fact]
    public async Task Should_ExecuteCoreOperations_WithConsoleEquivalentSemantics()
    {
        await using WebApplicationFactory<Program> factory = new();
        using HttpClient httpClient = factory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", "dev-local-api-key");
        FileSystemApiClient websiteClient = new(httpClient);

        string rootPath = "Root";
        string docsPath = "Root/Website_Ac023_Docs";
        string archivePath = "Root/Website_Ac023_Archive";
        string sourceFileName = "website-note.txt";
        string renamedFileName = "website-note-renamed.txt";

        OperationResultDto createDocs = await websiteClient.CreateDirectoryAsync(new CreateDirectoryRequestDto(rootPath, "Website_Ac023_Docs"));
        OperationResultDto createArchive = await websiteClient.CreateDirectoryAsync(new CreateDirectoryRequestDto(rootPath, "Website_Ac023_Archive"));
        OperationResultDto duplicateDocs = await websiteClient.CreateDirectoryAsync(new CreateDirectoryRequestDto(rootPath, "Website_Ac023_Docs"));

        Assert.True(createDocs.Success);
        Assert.True(createArchive.Success);
        Assert.False(duplicateDocs.Success);
        Assert.Contains("already exists", duplicateDocs.Message, StringComparison.OrdinalIgnoreCase);

        FormFile uploadFile = CreateTextFormFile(sourceFileName, "website-api-flow");
        OperationResultDto uploadResult = await websiteClient.UploadFileAsync(docsPath, uploadFile);
        Assert.True(uploadResult.Success);

        SizeCalculationResultDto sizeResult = await websiteClient.CalculateSizeAsync(docsPath);
        Assert.True(sizeResult.SizeBytes > 0);

        SearchResultDto searchResult = await websiteClient.SearchAsync("TXT");
        Assert.Contains(searchResult.Paths, path => path.EndsWith(sourceFileName, StringComparison.OrdinalIgnoreCase));

        XmlExportResultDto xmlResult = await websiteClient.ExportXmlAsync();
        Assert.Contains("Website_Ac023_Docs", xmlResult.XmlContent, StringComparison.Ordinal);

        OperationResultDto renameResult = await websiteClient.RenameFileAsync(new RenameFileRequestDto($"{docsPath}/{sourceFileName}", renamedFileName));
        Assert.True(renameResult.Success);

        OperationResultDto moveResult = await websiteClient.MoveFileAsync(new MoveFileRequestDto($"{docsPath}/{renamedFileName}", archivePath));
        Assert.True(moveResult.Success);

        OperationResultDto deleteMovedFile = await websiteClient.DeleteFileAsync(new DeleteFileRequestDto($"{archivePath}/{renamedFileName}"));
        Assert.True(deleteMovedFile.Success);

        OperationResultDto deleteDocs = await websiteClient.DeleteDirectoryAsync(new DeleteDirectoryRequestDto(docsPath));
        OperationResultDto deleteArchive = await websiteClient.DeleteDirectoryAsync(new DeleteDirectoryRequestDto(archivePath));
        Assert.True(deleteDocs.Success);
        Assert.True(deleteArchive.Success);

        FeatureFlagsResultDto flagsResult = await websiteClient.GetFeatureFlagsAsync();
        Assert.NotNull(flagsResult.Flags);
    }

    private static FormFile CreateTextFormFile(string fileName, string content)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
        MemoryStream stream = new(bytes);
        FormFile file = new(stream, 0, bytes.Length, "File", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        return file;
    }
}
