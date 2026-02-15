using Microsoft.AspNetCore.Http;

namespace CloudFileManager.Presentation.WebApi.Controllers;

/// <summary>
/// TemporaryUploadFileScope 類別，負責管理暫存上傳檔案生命週期。
/// </summary>
public sealed class TemporaryUploadFileScope : IDisposable
{
    /// <summary>
    /// 暫存檔案完整路徑。
    /// </summary>
    public string FilePath { get; }

    private TemporaryUploadFileScope(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// 建立暫存檔案並寫入表單檔案內容。
    /// </summary>
    public static async Task<TemporaryUploadFileScope> CreateAsync(IFormFile file, CancellationToken cancellationToken)
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "CloudFileManagerUploads");
        Directory.CreateDirectory(tempDirectory);

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string tempPath = Path.Combine(tempDirectory, $"{Guid.NewGuid():N}{extension}");

        await using (FileStream stream = File.Create(tempPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return new TemporaryUploadFileScope(tempPath);
    }

    /// <summary>
    /// 釋放暫存檔案。
    /// </summary>
    public void Dispose()
    {
        if (!File.Exists(FilePath))
        {
            return;
        }

        File.Delete(FilePath);
    }
}
