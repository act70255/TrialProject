using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudFileManager.Infrastructure.DataAccess.EfCore.Configurations;

/// <summary>
/// NodeTagEntityConfiguration 類別，負責定義與承載設定資料。
/// </summary>
public sealed class NodeTagEntityConfiguration : IEntityTypeConfiguration<NodeTagEntity>
{
    /// <summary>
    /// 設定資料。
    /// </summary>
    public void Configure(EntityTypeBuilder<NodeTagEntity> builder)
    {
        builder.ToTable("node_tags");

        builder.HasKey(item => item.Id);

        builder.HasOne(item => item.Tag)
            .WithMany(item => item.NodeTags)
            .HasForeignKey(item => item.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Directory)
            .WithMany(item => item.NodeTags)
            .HasForeignKey(item => item.DirectoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.File)
            .WithMany(item => item.NodeTags)
            .HasForeignKey(item => item.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => item.TagId);
        builder.HasIndex(item => item.DirectoryId);
        builder.HasIndex(item => item.FileId);

        builder.HasIndex(item => new { item.DirectoryId, item.TagId })
            .IsUnique()
            .HasFilter("DirectoryId IS NOT NULL");

        builder.HasIndex(item => new { item.FileId, item.TagId })
            .IsUnique()
            .HasFilter("FileId IS NOT NULL");

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_node_tags_single_target",
                "((DirectoryId IS NOT NULL AND FileId IS NULL) OR (DirectoryId IS NULL AND FileId IS NOT NULL))");
        });
    }
}
