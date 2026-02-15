using CloudFileManager.Infrastructure.DataAccess.EfCore;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudFileManager.Infrastructure.FileStorage;

/// <summary>
/// MetadataPathNormalizer 類別，負責正規化中繼資料路徑大小寫。
/// </summary>
public static class MetadataPathNormalizer
{
    /// <summary>
    /// 執行中繼資料路徑正規化。
    /// </summary>
    public static void Normalize(CloudFileDbContext dbContext, string storageRootPath)
    {
        NormalizeAsync(dbContext, storageRootPath).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 以非同步方式執行中繼資料路徑正規化。
    /// </summary>
    public static async Task NormalizeAsync(CloudFileDbContext dbContext, string storageRootPath, CancellationToken cancellationToken = default)
    {
        DirectoryEntity? root = await dbContext.Directories.FirstOrDefaultAsync(item => item.ParentId == null && item.Name == "Root", cancellationToken);
        if (root is null)
        {
            return;
        }

        bool hasChanges = false;
        if (!string.IsNullOrEmpty(root.RelativePath))
        {
            root.RelativePath = string.Empty;
            hasChanges = true;
        }

        List<DirectoryEntity> directories = await dbContext.Directories.ToListAsync(cancellationToken);
        Dictionary<Guid, List<DirectoryEntity>> childrenByParent = directories
            .Where(item => item.ParentId is not null)
            .GroupBy(item => item.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        Queue<DirectoryEntity> queue = new();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            DirectoryEntity current = queue.Dequeue();
            if (!childrenByParent.TryGetValue(current.Id, out List<DirectoryEntity>? children))
            {
                continue;
            }

            foreach (DirectoryEntity child in children)
            {
                string currentPhysicalPath = ResolvePhysicalPath(storageRootPath, current.RelativePath);
                string resolvedPath = BuildStoredPath(current.RelativePath, child.Name);

                if (Directory.Exists(currentPhysicalPath))
                {
                    DirectoryInfo? matched = new DirectoryInfo(currentPhysicalPath)
                        .EnumerateDirectories()
                        .FirstOrDefault(item => string.Equals(item.Name, child.Name, StringComparison.OrdinalIgnoreCase));

                    if (matched is not null)
                    {
                        if (!string.Equals(child.Name, matched.Name, StringComparison.Ordinal))
                        {
                            child.Name = matched.Name;
                            hasChanges = true;
                        }

                        resolvedPath = BuildStoredPath(current.RelativePath, matched.Name);
                    }
                }

                if (!string.Equals(child.RelativePath, resolvedPath, StringComparison.Ordinal))
                {
                    child.RelativePath = resolvedPath;
                    hasChanges = true;
                }

                queue.Enqueue(child);
            }
        }

        List<FileEntity> files = await dbContext.Files.ToListAsync(cancellationToken);
        Dictionary<Guid, DirectoryEntity> directoryMap = directories.ToDictionary(item => item.Id);
        foreach (FileEntity file in files)
        {
            if (!directoryMap.TryGetValue(file.DirectoryId, out DirectoryEntity? parent))
            {
                continue;
            }

            string parentPhysicalPath = ResolvePhysicalPath(storageRootPath, parent.RelativePath);
            string resolvedPath = BuildStoredPath(parent.RelativePath, file.Name);
            if (Directory.Exists(parentPhysicalPath))
            {
                FileInfo? matched = new DirectoryInfo(parentPhysicalPath)
                    .EnumerateFiles()
                    .FirstOrDefault(item => string.Equals(item.Name, file.Name, StringComparison.OrdinalIgnoreCase));

                if (matched is not null)
                {
                    if (!string.Equals(file.Name, matched.Name, StringComparison.Ordinal))
                    {
                        file.Name = matched.Name;
                        hasChanges = true;
                    }

                    resolvedPath = BuildStoredPath(parent.RelativePath, matched.Name);
                }
            }

            if (!string.Equals(file.RelativePath, resolvedPath, StringComparison.Ordinal))
            {
                file.RelativePath = resolvedPath;
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string ResolvePhysicalPath(string storageRootPath, string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return storageRootPath;
        }

        if (Path.IsPathRooted(storedPath))
        {
            return Path.GetFullPath(storedPath);
        }

        string normalized = storedPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(storageRootPath, normalized));
    }

    private static string BuildStoredPath(string parentStoredPath, string nodeName)
    {
        if (string.IsNullOrWhiteSpace(parentStoredPath))
        {
            return nodeName;
        }

        return $"{parentStoredPath.TrimEnd('/', '\\')}/{nodeName}";
    }
}
