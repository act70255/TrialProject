using CloudFileManager.Infrastructure.DataAccess.EfCore;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudFileManager.Infrastructure.FileStorage;

/// <summary>
/// StorageNameConflictQueries 類別，負責名稱衝突查詢。
/// </summary>
public static class StorageNameConflictQueries
{
    /// <summary>
    /// 判斷是否具有目錄名稱衝突。
    /// </summary>
    public static bool HasDirectoryNameConflict(CloudFileDbContext dbContext, Guid? parentId, string candidateName, Guid? excludeDirectoryId)
    {
        string expected = candidateName.Trim();
        string? caseInsensitiveCollation = ResolveCaseInsensitiveCollation(dbContext);
        IQueryable<DirectoryEntity> query = dbContext.Directories
            .Where(item => item.ParentId == parentId && (!excludeDirectoryId.HasValue || item.Id != excludeDirectoryId.Value));

        if (caseInsensitiveCollation is not null)
        {
            return query.Any(item => EF.Functions.Collate(item.Name, caseInsensitiveCollation) == expected);
        }

        return query.Any(item => item.Name == expected);
    }

    /// <summary>
    /// 判斷是否具有檔案名稱衝突。
    /// </summary>
    public static bool HasFileNameConflict(CloudFileDbContext dbContext, Guid directoryId, string candidateName, Guid? excludeFileId)
    {
        string expected = candidateName.Trim();
        string? caseInsensitiveCollation = ResolveCaseInsensitiveCollation(dbContext);
        IQueryable<FileEntity> query = dbContext.Files
            .Where(item => item.DirectoryId == directoryId && (!excludeFileId.HasValue || item.Id != excludeFileId.Value));

        if (caseInsensitiveCollation is not null)
        {
            return query.Any(item => EF.Functions.Collate(item.Name, caseInsensitiveCollation) == expected);
        }

        return query.Any(item => item.Name == expected);
    }

    /// <summary>
    /// 查找檔案By名稱。
    /// </summary>
    public static FileEntity? FindFileByName(CloudFileDbContext dbContext, Guid directoryId, string fileName, Guid? excludeFileId)
    {
        string expected = fileName.Trim();
        string? caseInsensitiveCollation = ResolveCaseInsensitiveCollation(dbContext);
        IQueryable<FileEntity> query = dbContext.Files
            .Where(item => item.DirectoryId == directoryId && (!excludeFileId.HasValue || item.Id != excludeFileId.Value));

        if (caseInsensitiveCollation is not null)
        {
            return query.FirstOrDefault(item => EF.Functions.Collate(item.Name, caseInsensitiveCollation) == expected);
        }

        return query.FirstOrDefault(item => item.Name == expected);
    }

    private static string? ResolveCaseInsensitiveCollation(CloudFileDbContext dbContext)
    {
        if (StorageDbProviderClassifier.IsSqlite(dbContext))
        {
            return "NOCASE";
        }

        if (StorageDbProviderClassifier.IsSqlServer(dbContext))
        {
            return "SQL_Latin1_General_CP1_CI_AS";
        }

        return null;
    }
}
