using CloudFileManager.Application.Configuration;

namespace CloudFileManager.Infrastructure.FileStorage;

/// <summary>
/// StorageBootstrapper 類別，負責儲存路徑解析與目錄初始化。
/// </summary>
public static class StorageBootstrapper
{
    /// <summary>
    /// 解析 Storage 根目錄完整路徑。
    /// </summary>
    public static string ResolveStorageRootPath(string configuredPath, string basePath)
    {
        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(basePath, configuredPath));
    }

    /// <summary>
    /// 確保 Storage 根目錄存在。
    /// </summary>
    public static string EnsureStorageRoot(AppConfig config, string basePath)
    {
        string resolvedPath = ResolveStorageRootPath(config.Storage.StorageRootPath, basePath);

        if (!Directory.Exists(resolvedPath))
        {
            Directory.CreateDirectory(resolvedPath);
        }

        return resolvedPath;
    }
}
