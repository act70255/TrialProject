using CloudFileManager.Application.Models;
using CloudFileManager.Domain;

namespace CloudFileManager.Application.Implementations;

/// <summary>
/// TextFileFactory，建立文字檔案實體。
/// </summary>
public sealed class TextFileFactory : ICloudFileFactory
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt"
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
        string encoding = string.IsNullOrWhiteSpace(request.Encoding) ? "UTF-8" : request.Encoding;
        return new TextFile(request.FileName, request.Size, createdTime, encoding);
    }
}
