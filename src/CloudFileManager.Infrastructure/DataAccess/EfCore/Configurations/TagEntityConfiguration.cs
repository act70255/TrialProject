using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudFileManager.Infrastructure.DataAccess.EfCore.Configurations;

/// <summary>
/// TagEntityConfiguration 類別，負責定義與承載設定資料。
/// </summary>
public sealed class TagEntityConfiguration : IEntityTypeConfiguration<TagEntity>
{
    /// <summary>
    /// 設定資料。
    /// </summary>
    public void Configure(EntityTypeBuilder<TagEntity> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Name)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(item => item.Color)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(item => item.Name)
            .IsUnique();

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_tags_name", "Name IN ('Urgent', 'Work', 'Personal')");
            tableBuilder.HasCheckConstraint("CK_tags_color", "Color IN ('Red', 'Blue', 'Green')");
        });

        builder.HasData(
            new TagEntity
            {
                Id = Guid.Parse("2c2c123c-45d6-4a89-8b9d-4d3b7fd72111"),
                Name = "Urgent",
                Color = "Red",
                CreatedTime = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new TagEntity
            {
                Id = Guid.Parse("74457bd5-0709-4afb-8a2f-7209f19766b6"),
                Name = "Work",
                Color = "Blue",
                CreatedTime = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new TagEntity
            {
                Id = Guid.Parse("17d22f5d-7fd0-4975-8f56-ce8ac2aa42e8"),
                Name = "Personal",
                Color = "Green",
                CreatedTime = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
            });
    }
}
