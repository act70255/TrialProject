using CloudFileManager.Application.Models;
using CloudFileManager.Domain;

namespace CloudFileManager.Application.Implementations;

/// <summary>
/// CloudFileFactoryRegistry，管理 CloudFile 建立策略。
/// </summary>
public sealed class CloudFileFactoryRegistry
{
    private readonly IReadOnlyList<ICloudFileFactory> _factories;

    /// <summary>
    /// 初始化 CloudFileFactoryRegistry。
    /// </summary>
    public CloudFileFactoryRegistry(IReadOnlyList<ICloudFileFactory> factories)
    {
        _factories = factories;
    }

    /// <summary>
    /// 建立對應的檔案實體。
    /// </summary>
    public CloudFile Create(UploadFileRequest request, DateTime createdTime)
    {
        string extension = Path.GetExtension(request.FileName).ToLowerInvariant();

        ICloudFileFactory? factory = _factories.FirstOrDefault(item => item.CanCreate(extension));
        if (factory is null)
        {
            throw new InvalidOperationException($"Unsupported file extension: {extension}");
        }

        return factory.Create(request, createdTime);
    }
}
