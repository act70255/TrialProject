using CloudFileManager.Presentation.WebApi.Controllers;
using Microsoft.AspNetCore.Http;

namespace CloudFileManager.IntegrationTests;

/// <summary>
/// TemporaryUploadFileScopeTests 類別，負責驗證暫存上傳檔案生命週期。
/// </summary>
public sealed class TemporaryUploadFileScopeTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateAndDeleteTemporaryFile()
    {
        await using MemoryStream stream = new("hello-upload"u8.ToArray());
        IFormFile formFile = new FormFile(stream, 0, stream.Length, "file", "hello.txt");

        string path;
        using (TemporaryUploadFileScope scope = await TemporaryUploadFileScope.CreateAsync(formFile, CancellationToken.None))
        {
            path = scope.FilePath;
            Assert.True(File.Exists(path));
            Assert.EndsWith(".txt", path, StringComparison.OrdinalIgnoreCase);
        }

        Assert.False(File.Exists(path));
    }
}
