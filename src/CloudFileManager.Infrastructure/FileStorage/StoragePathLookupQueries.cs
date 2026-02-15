using CloudFileManager.Infrastructure.DataAccess.EfCore;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudFileManager.Infrastructure.FileStorage;

/// <summary>
/// StoragePathLookupQueries 類別，負責路徑查詢。
/// </summary>
public static class StoragePathLookupQueries
{
    /// <summary>
    /// 依目錄路徑查詢目錄。
    /// </summary>
    public static DirectoryEntity? FindDirectoryByPath(CloudFileDbContext dbContext, string path)
    {
        string[] segments = SplitPath(path);
        if (segments.Length == 0 || !segments[0].Equals("Root", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        Guid? directoryId = FindDirectoryIdByPath(dbContext, segments);
        if (!directoryId.HasValue)
        {
            return null;
        }

        return dbContext.Directories.FirstOrDefault(item => item.Id == directoryId.Value);
    }

    /// <summary>
    /// 依檔案路徑查詢檔案與父目錄。
    /// </summary>
    public static (DirectoryEntity? Directory, FileEntity? File) FindFileByPath(CloudFileDbContext dbContext, string filePath)
    {
        string[] segments = SplitPath(filePath);
        if (segments.Length < 2)
        {
            return (null, null);
        }

        string directoryPath = string.Join('/', segments[..^1]);
        string fileName = segments[^1];

        Guid? directoryId = FindDirectoryIdByPath(dbContext, segments[..^1]);
        if (!directoryId.HasValue)
        {
            return (null, null);
        }

        DirectoryEntity? directory = dbContext.Directories.FirstOrDefault(item => item.Id == directoryId.Value);
        if (directory is null)
        {
            return (null, null);
        }

        FileEntity? file = FindFileInDirectoryByName(dbContext, directory.Id, fileName);
        return (directory, file);
    }

    private static Guid? FindDirectoryIdByPath(CloudFileDbContext dbContext, string[] segments)
    {
        List<DirectoryPathNode> nodes = dbContext.Directories
            .AsNoTracking()
            .Select(item => new DirectoryPathNode(item.Id, item.ParentId, item.Name))
            .ToList();

        Dictionary<string, Dictionary<string, Guid>> childrenByParent = BuildDirectoryIndex(nodes);

        if (!childrenByParent.TryGetValue(ToParentKey(null), out Dictionary<string, Guid>? rootDirectories) ||
            !rootDirectories.TryGetValue("Root", out Guid currentId))
        {
            return null;
        }

        for (int index = 1; index < segments.Length; index++)
        {
            if (!childrenByParent.TryGetValue(ToParentKey(currentId), out Dictionary<string, Guid>? children) ||
                !children.TryGetValue(segments[index], out Guid nextId))
            {
                return null;
            }

            currentId = nextId;
        }

        return currentId;
    }

    private static Dictionary<string, Dictionary<string, Guid>> BuildDirectoryIndex(IEnumerable<DirectoryPathNode> nodes)
    {
        Dictionary<string, Dictionary<string, Guid>> childrenByParent = new(StringComparer.Ordinal);

        foreach (DirectoryPathNode node in nodes)
        {
            string parentKey = ToParentKey(node.ParentId);
            if (!childrenByParent.TryGetValue(parentKey, out Dictionary<string, Guid>? children))
            {
                children = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
                childrenByParent[parentKey] = children;
            }

            children[node.Name] = node.Id;
        }

        return childrenByParent;
    }

    private static string ToParentKey(Guid? parentId)
    {
        return parentId.HasValue ? parentId.Value.ToString("N") : "ROOT";
    }

    private static FileEntity? FindFileInDirectoryByName(CloudFileDbContext dbContext, Guid directoryId, string fileName)
    {
        IQueryable<FileEntity> query = dbContext.Files.Where(item => item.DirectoryId == directoryId);

        if (StorageDbProviderClassifier.IsSqlite(dbContext))
        {
            return query.FirstOrDefault(item => EF.Functions.Collate(item.Name, "NOCASE") == fileName);
        }

        if (StorageDbProviderClassifier.IsSqlServer(dbContext))
        {
            return query.FirstOrDefault(item => EF.Functions.Collate(item.Name, "SQL_Latin1_General_CP1_CI_AS") == fileName);
        }

        return query.FirstOrDefault(item => item.Name == fileName);
    }

    private static string[] SplitPath(string path)
    {
        return path
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private sealed record DirectoryPathNode(Guid Id, Guid? ParentId, string Name);
}
