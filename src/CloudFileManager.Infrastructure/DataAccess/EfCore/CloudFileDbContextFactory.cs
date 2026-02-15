using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CloudFileManager.Infrastructure.DataAccess.EfCore;

/// <summary>
/// CloudFileDbContextFactory，提供設計階段 DbContext 建立。
/// </summary>
public sealed class CloudFileDbContextFactory : IDesignTimeDbContextFactory<CloudFileDbContext>
{
    /// <summary>
    /// 建立資料庫內容物件。
    /// </summary>
    public CloudFileDbContext CreateDbContext(string[] args)
    {
        string basePath = AppContext.BaseDirectory;
        string databasePath = Path.GetFullPath(Path.Combine(basePath, "./data/cloud-file-manager.db"));
        string? directory = Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        DbContextOptionsBuilder<CloudFileDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlite($"Data Source={databasePath}");

        return new CloudFileDbContext(optionsBuilder.Options);
    }
}
