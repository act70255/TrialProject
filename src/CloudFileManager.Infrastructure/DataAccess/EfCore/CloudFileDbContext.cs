using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudFileManager.Infrastructure.DataAccess.EfCore;

/// <summary>
/// CloudFileDbContext，定義 EF Core 實體集合與映射入口。
/// </summary>
public sealed class CloudFileDbContext : DbContext
{
    /// <summary>
    /// 初始化 CloudFileDbContext。
    /// </summary>
    public CloudFileDbContext(DbContextOptions<CloudFileDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// 目錄實體集合。
    /// </summary>
    public DbSet<DirectoryEntity> Directories => Set<DirectoryEntity>();

    /// <summary>
    /// 檔案實體集合。
    /// </summary>
    public DbSet<FileEntity> Files => Set<FileEntity>();

    /// <summary>
    /// 檔案中繼資料。
    /// </summary>
    public DbSet<FileMetadataEntity> FileMetadata => Set<FileMetadataEntity>();

    /// <summary>
    /// 標籤主檔集合。
    /// </summary>
    public DbSet<TagEntity> Tags => Set<TagEntity>();

    /// <summary>
    /// 節點標籤關聯集合。
    /// </summary>
    public DbSet<NodeTagEntity> NodeTags => Set<NodeTagEntity>();

    /// <summary>
    /// 套用實體映射設定。
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CloudFileDbContext).Assembly);
    }
}
