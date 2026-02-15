using CloudFileManager.Infrastructure.DataAccess.EfCore;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;

namespace CloudFileManager.Infrastructure.FileStorage;

/// <summary>
/// StorageDirectoryMutations 類別，負責目錄樹異動。
/// </summary>
public static class StorageDirectoryMutations
{
    /// <summary>
    /// 遞迴移除目錄與子節點。
    /// </summary>
    public static void RemoveDirectoryCascade(CloudFileDbContext dbContext, Guid directoryId)
    {
        List<Guid> childIds = dbContext.Directories
            .Where(item => item.ParentId == directoryId)
            .Select(item => item.Id)
            .ToList();

        foreach (Guid childId in childIds)
        {
            RemoveDirectoryCascade(dbContext, childId);
        }

        List<FileEntity> files = dbContext.Files.Where(item => item.DirectoryId == directoryId).ToList();
        foreach (FileEntity file in files)
        {
            FileMetadataEntity? metadata = dbContext.FileMetadata.FirstOrDefault(item => item.FileId == file.Id);
            if (metadata is not null)
            {
                dbContext.FileMetadata.Remove(metadata);
            }

            dbContext.Files.Remove(file);
        }

        DirectoryEntity? directory = dbContext.Directories.FirstOrDefault(item => item.Id == directoryId);
        if (directory is not null)
        {
            dbContext.Directories.Remove(directory);
        }
    }

    /// <summary>
    /// 更新子孫節點實體路徑。
    /// </summary>
    public static void UpdateDescendantPhysicalPaths(CloudFileDbContext dbContext, Guid parentId, string parentPhysicalPath)
    {
        List<DirectoryEntity> childDirectories = dbContext.Directories
            .Where(item => item.ParentId == parentId)
            .ToList();

        foreach (DirectoryEntity child in childDirectories)
        {
            child.RelativePath = CombineStoredPath(parentPhysicalPath, child.Name);
            UpdateDescendantPhysicalPaths(dbContext, child.Id, child.RelativePath);
        }

        List<FileEntity> files = dbContext.Files
            .Where(item => item.DirectoryId == parentId)
            .ToList();

        foreach (FileEntity file in files)
        {
            file.RelativePath = CombineStoredPath(parentPhysicalPath, file.Name);
        }
    }

    private static string CombineStoredPath(string parentPath, string nodeName)
    {
        if (string.IsNullOrWhiteSpace(parentPath))
        {
            return nodeName;
        }

        return $"{parentPath.TrimEnd('/', '\\')}/{nodeName}";
    }
}
