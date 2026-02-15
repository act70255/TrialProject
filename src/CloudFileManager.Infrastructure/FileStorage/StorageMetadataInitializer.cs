using CloudFileManager.Infrastructure.DataAccess.EfCore;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudFileManager.Infrastructure.FileStorage;

/// <summary>
/// StorageMetadataInitializer 類別，負責啟動時中繼資料初始化。
/// </summary>
public static class StorageMetadataInitializer
{
    /// <summary>
    /// 以非同步方式初始化中繼資料與路徑正規化。
    /// </summary>
    public static async Task InitializeAsync(CloudFileDbContext dbContext, string storageRootPath, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(storageRootPath);

        bool rootExists = await dbContext.Directories.AnyAsync(item => item.ParentId == null && item.Name == "Root", cancellationToken);
        if (!rootExists)
        {
            dbContext.Directories.Add(new DirectoryEntity
            {
                Id = Guid.NewGuid(),
                ParentId = null,
                Name = "Root",
                CreatedTime = DateTime.UtcNow,
                CreationOrder = 1,
                RelativePath = string.Empty
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await MetadataPathNormalizer.NormalizeAsync(dbContext, storageRootPath, cancellationToken);
    }
}
