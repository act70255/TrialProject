using CloudFileManager.Application.Models;
using CloudFileManager.Domain;

namespace CloudFileManager.Application.Implementations;

/// <summary>
/// ImageFileFactory，建立影像檔案實體。
/// </summary>
public sealed class ImageFileFactory : ICloudFileFactory
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg"
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
        int width = request.Width.GetValueOrDefault(1920);
        int height = request.Height.GetValueOrDefault(1080);
        return new ImageFile(request.FileName, request.Size, createdTime, width, height);
    }
}
