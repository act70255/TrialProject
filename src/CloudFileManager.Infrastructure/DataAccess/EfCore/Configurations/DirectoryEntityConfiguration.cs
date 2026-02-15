using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudFileManager.Infrastructure.DataAccess.EfCore.Configurations;

/// <summary>
/// 目錄EntityConfiguration 類別，負責定義與承載設定資料。
/// </summary>
public sealed class DirectoryEntityConfiguration : IEntityTypeConfiguration<DirectoryEntity>
{
    /// <summary>
    /// 設定資料。
    /// </summary>
    public void Configure(EntityTypeBuilder<DirectoryEntity> builder)
    {
        builder.ToTable("directories");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(item => item.RelativePath)
            .IsRequired()
            .HasMaxLength(1024);

        builder.HasIndex(item => new { item.ParentId, item.Name })
            .IsUnique();

        builder.HasIndex(item => new { item.ParentId, item.CreationOrder });

        builder.HasOne(item => item.Parent)
            .WithMany(item => item.Children)
            .HasForeignKey(item => item.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
