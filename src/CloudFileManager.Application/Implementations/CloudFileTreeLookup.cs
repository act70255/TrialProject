using CloudFileManager.Domain;

namespace CloudFileManager.Application.Implementations;

/// <summary>
/// CloudFileTreeLookup 類別，負責樹狀結構查找。
/// </summary>
public static class CloudFileTreeLookup
{
    /// <summary>
    /// 依路徑查找目錄節點。
    /// </summary>
    public static CloudDirectory? FindDirectory(CloudDirectory root, string path)
    {
        string[] segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
        {
            return null;
        }

        if (!segments[0].Equals(root.Name, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        CloudDirectory current = root;
        for (int index = 1; index < segments.Length; index++)
        {
            CloudDirectory? next = current.Directories.FirstOrDefault(item => item.Name.Equals(segments[index], StringComparison.OrdinalIgnoreCase));
            if (next is null)
            {
                return null;
            }

            current = next;
        }

        return current;
    }

    /// <summary>
    /// 依檔案路徑取得檔案與其父目錄。
    /// </summary>
    public static (CloudDirectory? ParentDirectory, CloudFile? File) FindFileWithParent(CloudDirectory root, string filePath)
    {
        string normalizedPath = filePath.Replace('\\', '/').Trim().TrimEnd('/');
        int separatorIndex = normalizedPath.LastIndexOf('/');
        if (separatorIndex <= 0)
        {
            return (null, null);
        }

        string directoryPath = normalizedPath[..separatorIndex];
        string fileName = normalizedPath[(separatorIndex + 1)..];

        CloudDirectory? directory = FindDirectory(root, directoryPath);
        if (directory is null)
        {
            return (null, null);
        }

        CloudFile? file = directory.Files.FirstOrDefault(item => item.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        return (directory, file);
    }

    /// <summary>
    /// 依目錄路徑取得目錄與其父目錄。
    /// </summary>
    public static (CloudDirectory? Parent, CloudDirectory? Directory) FindDirectoryWithParent(CloudDirectory root, string directoryPath)
    {
        string normalizedPath = directoryPath.Replace('\\', '/').Trim().TrimEnd('/');
        int separatorIndex = normalizedPath.LastIndexOf('/');
        if (separatorIndex <= 0)
        {
            return (null, null);
        }

        string parentPath = normalizedPath[..separatorIndex];
        string name = normalizedPath[(separatorIndex + 1)..];

        CloudDirectory? parent = FindDirectory(root, parentPath);
        if (parent is null)
        {
            return (null, null);
        }

        CloudDirectory? directory = parent.Directories.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return (parent, directory);
    }
}
