using CloudFileManager.Application.Models;
using CloudFileManager.Domain;

namespace CloudFileManager.Application.Implementations;

/// <summary>
/// WordFileFactory，建立 Word 檔案實體。
/// </summary>
public sealed class WordFileFactory : ICloudFileFactory
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".docx"
    };

    /// <summary>
    /// 判斷是否可建立對應檔案實體。
    /// </summary>
    public bool CanCreate(string extension) => AllowedExtensions.Contains(extension);

    /// <summary>
    /// 建立對應的檔案實體。
    /// </summary>
    public CloudFile Create(UploadFileRequest request, DateTime createdTime)
    {
        int pageCount = request.PageCount.GetValueOrDefault(1);
        return new WordFile(request.FileName, request.Size, createdTime, pageCount);
    }
}
