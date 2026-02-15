using CloudFileManager.Infrastructure.DataAccess.EfCore;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using CloudFileManager.Infrastructure.FileStorage;
using Microsoft.EntityFrameworkCore;

namespace CloudFileManager.UnitTests;

public sealed class MetadataPathNormalizerTests
{
    [Fact]
    public void Normalize_ShouldReturn_WhenRootDirectoryDoesNotExistInMetadata()
    {
        using CloudFileDbContext dbContext = CreateDbContext();

        MetadataPathNormalizer.Normalize(dbContext, "C:/tmp/root");

        Assert.Empty(dbContext.Directories);
        Assert.Empty(dbContext.Files);
    }

    [Fact]
    public void Normalize_ShouldFixDirectoryAndFileNameCasing_AndRelativePaths()
    {
        string storageRootPath = Path.Combine(Path.GetTempPath(), $"cfm-normalizer-{Guid.NewGuid():N}");
        string actualChildName = "Docs";
        string actualFileName = "Report.TXT";

        string childPath = Path.Combine(storageRootPath, actualChildName);
        Directory.CreateDirectory(childPath);
        string filePath = Path.Combine(childPath, actualFileName);
        File.WriteAllText(filePath, "content");

        try
        {
            Guid rootId = Guid.NewGuid();
            Guid childId = Guid.NewGuid();
            Guid fileId = Guid.NewGuid();

            using CloudFileDbContext dbContext = CreateDbContext();

            dbContext.Directories.AddRange(
                new DirectoryEntity
                {
                    Id = rootId,
                    ParentId = null,
                    Name = "Root",
                    RelativePath = "old-root-path",
                    CreatedTime = DateTime.UtcNow,
                    CreationOrder = 1
                },
                new DirectoryEntity
                {
                    Id = childId,
                    ParentId = rootId,
                    Name = "docs",
                    RelativePath = "old-child-path",
                    CreatedTime = DateTime.UtcNow,
                    CreationOrder = 1
                });

            dbContext.Files.Add(new FileEntity
            {
                Id = fileId,
                DirectoryId = childId,
                Name = "report.txt",
                Extension = ".txt",
                SizeBytes = 10,
                CreatedTime = DateTime.UtcNow,
                FileType = 3,
                CreationOrder = 1,
                RelativePath = "old-file-path"
            });

            dbContext.SaveChanges();

            MetadataPathNormalizer.Normalize(dbContext, storageRootPath);

            DirectoryEntity root = dbContext.Directories.Single(item => item.Id == rootId);
            DirectoryEntity child = dbContext.Directories.Single(item => item.Id == childId);
            FileEntity file = dbContext.Files.Single(item => item.Id == fileId);

            Assert.Equal(string.Empty, root.RelativePath);
            Assert.Equal(actualChildName, child.Name);
            Assert.Equal(actualChildName, child.RelativePath);
            Assert.Equal(actualFileName, file.Name);
            Assert.Equal($"{actualChildName}/{actualFileName}", file.RelativePath);
        }
        finally
        {
            if (Directory.Exists(storageRootPath))
            {
                Directory.Delete(storageRootPath, recursive: true);
            }
        }
    }

    private static CloudFileDbContext CreateDbContext()
    {
        DbContextOptions<CloudFileDbContext> options = new DbContextOptionsBuilder<CloudFileDbContext>()
            .UseInMemoryDatabase($"cfm-normalizer-tests-{Guid.NewGuid():N}")
            .Options;

        return new CloudFileDbContext(options);
    }
}
