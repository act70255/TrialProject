using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudFileManager.Infrastructure.DataAccess.EfCore.Configurations;

/// <summary>
/// 檔案MetadataEntityConfiguration 類別，負責定義與承載設定資料。
/// </summary>
public sealed class FileMetadataEntityConfiguration : IEntityTypeConfiguration<FileMetadataEntity>
{
    /// <summary>
    /// 設定資料。
    /// </summary>
    public void Configure(EntityTypeBuilder<FileMetadataEntity> builder)
    {
        builder.ToTable("file_metadata");

        builder.HasKey(item => item.FileId);

        builder.Property(item => item.Encoding)
            .HasMaxLength(50);

        builder.HasOne(item => item.File)
            .WithOne(item => item.Metadata)
            .HasForeignKey<FileMetadataEntity>(item => new { item.FileId, item.FileType })
            .HasPrincipalKey<FileEntity>(item => new { item.Id, item.FileType })
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_file_metadata_type_fields",
                "(" +
                "(FileType = 1 AND PageCount IS NOT NULL AND Width IS NULL AND Height IS NULL AND Encoding IS NULL) OR " +
                "(FileType = 2 AND PageCount IS NULL AND Width IS NOT NULL AND Height IS NOT NULL AND Encoding IS NULL) OR " +
                "(FileType = 3 AND PageCount IS NULL AND Width IS NULL AND Height IS NULL AND Encoding IS NOT NULL)" +
                ")");
        });
    }
}
