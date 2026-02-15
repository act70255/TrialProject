using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudFileManager.Infrastructure.DataAccess.EfCore.Configurations;

/// <summary>
/// 檔案EntityConfiguration 類別，負責定義與承載設定資料。
/// </summary>
public sealed class FileEntityConfiguration : IEntityTypeConfiguration<FileEntity>
{
    /// <summary>
    /// 設定資料。
    /// </summary>
    public void Configure(EntityTypeBuilder<FileEntity> builder)
    {
        builder.ToTable("files");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(item => item.Extension)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(item => item.RelativePath)
            .IsRequired()
            .HasMaxLength(1024);

        builder.HasOne(item => item.Directory)
            .WithMany(item => item.Files)
            .HasForeignKey(item => item.DirectoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => new { item.DirectoryId, item.Name })
            .IsUnique();

        builder.HasIndex(item => new { item.DirectoryId, item.CreationOrder });
        builder.HasIndex(item => item.Extension);
        builder.HasIndex(item => item.FileType);
        builder.HasIndex(item => item.RelativePath);

        builder.HasAlternateKey(item => new { item.Id, item.FileType });

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_files_size_bytes", "SizeBytes >= 0");
            tableBuilder.HasCheckConstraint("CK_files_extension", "Extension = lower(Extension) AND Extension LIKE '.%'");
            tableBuilder.HasCheckConstraint("CK_files_file_type", "FileType IN (1, 2, 3)");
        });
    }
}
