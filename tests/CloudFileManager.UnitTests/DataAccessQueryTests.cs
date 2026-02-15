using CloudFileManager.Infrastructure.DataAccess.EfCore;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using CloudFileManager.Infrastructure.FileStorage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CloudFileManager.UnitTests;

public sealed class DataAccessQueryTests
{
    [Fact]
    public void StoragePathLookupQueries_ShouldResolveDirectoryAndFilePath_OnSqlite()
    {
        using TestSqliteContextScope scope = TestSqliteContextScope.Create();
        CloudFileDbContext dbContext = scope.DbContext;

        SeedTree(dbContext, out Guid docsId, out Guid fileId);

        DirectoryEntity? docs = StoragePathLookupQueries.FindDirectoryByPath(dbContext, "root/docs");
        (DirectoryEntity? directory, FileEntity? file) = StoragePathLookupQueries.FindFileByPath(dbContext, "Root/Docs/README.TXT");
        DirectoryEntity? invalidRoot = StoragePathLookupQueries.FindDirectoryByPath(dbContext, "Other/Docs");

        Assert.NotNull(docs);
        Assert.Equal(docsId, docs!.Id);

        Assert.NotNull(directory);
        Assert.NotNull(file);
        Assert.Equal(docsId, directory!.Id);
        Assert.Equal(fileId, file!.Id);

        Assert.Null(invalidRoot);
    }

    [Fact]
    public void StorageNameConflictQueries_ShouldDetectCaseInsensitiveConflicts_OnSqlite()
    {
        using TestSqliteContextScope scope = TestSqliteContextScope.Create();
        CloudFileDbContext dbContext = scope.DbContext;

        SeedTree(dbContext, out Guid docsId, out Guid fileId);

        bool directoryConflict = StorageNameConflictQueries.HasDirectoryNameConflict(dbContext, parentId: null, " root ", excludeDirectoryId: null);
        bool fileConflict = StorageNameConflictQueries.HasFileNameConflict(dbContext, docsId, " readme.txt ", excludeFileId: null);
        bool fileConflictExcluded = StorageNameConflictQueries.HasFileNameConflict(dbContext, docsId, "readme.txt", excludeFileId: fileId);
        FileEntity? foundFile = StorageNameConflictQueries.FindFileByName(dbContext, docsId, " readme.txt ", excludeFileId: null);

        Assert.True(directoryConflict);
        Assert.True(fileConflict);
        Assert.False(fileConflictExcluded);
        Assert.NotNull(foundFile);
        Assert.Equal(fileId, foundFile!.Id);
    }

    [Fact]
    public void StorageNameConflictQueries_ShouldUseFallbackComparison_OnNonRelationalProvider()
    {
        using TestInMemoryContextScope scope = TestInMemoryContextScope.Create();
        CloudFileDbContext dbContext = scope.DbContext;

        SeedTree(dbContext, out Guid docsId, out Guid fileId);

        bool exactMatch = StorageNameConflictQueries.HasFileNameConflict(dbContext, docsId, "Readme.txt", excludeFileId: null);
        bool differentCase = StorageNameConflictQueries.HasFileNameConflict(dbContext, docsId, "readme.txt", excludeFileId: null);
        FileEntity? foundExact = StorageNameConflictQueries.FindFileByName(dbContext, docsId, "Readme.txt", excludeFileId: null);
        FileEntity? foundDifferentCase = StorageNameConflictQueries.FindFileByName(dbContext, docsId, "readme.txt", excludeFileId: null);
        bool excluded = StorageNameConflictQueries.HasFileNameConflict(dbContext, docsId, "Readme.txt", excludeFileId: fileId);

        Assert.True(exactMatch);
        Assert.False(differentCase);
        Assert.NotNull(foundExact);
        Assert.Null(foundDifferentCase);
        Assert.False(excluded);
    }

    private static void SeedTree(CloudFileDbContext dbContext, out Guid docsId, out Guid fileId)
    {
        Guid rootId = Guid.NewGuid();
        docsId = Guid.NewGuid();
        fileId = Guid.NewGuid();

        dbContext.Directories.AddRange(
            new DirectoryEntity
            {
                Id = rootId,
                ParentId = null,
                Name = "Root",
                CreatedTime = DateTime.UtcNow,
                CreationOrder = 1,
                RelativePath = Path.Combine(Path.GetTempPath(), "root")
            },
            new DirectoryEntity
            {
                Id = docsId,
                ParentId = rootId,
                Name = "Docs",
                CreatedTime = DateTime.UtcNow,
                CreationOrder = 1,
                RelativePath = Path.Combine(Path.GetTempPath(), "root", "Docs")
            });

        dbContext.Files.Add(new FileEntity
        {
            Id = fileId,
            DirectoryId = docsId,
            Name = "Readme.txt",
            Extension = ".txt",
            SizeBytes = 1,
            CreatedTime = DateTime.UtcNow,
            FileType = 3,
            CreationOrder = 1,
            RelativePath = Path.Combine(Path.GetTempPath(), "root", "Docs", "Readme.txt")
        });

        dbContext.SaveChanges();
    }

    private sealed class TestSqliteContextScope : IDisposable
    {
        private readonly SqliteConnection _connection;

        private TestSqliteContextScope(SqliteConnection connection, CloudFileDbContext dbContext)
        {
            _connection = connection;
            DbContext = dbContext;
        }

        public CloudFileDbContext DbContext { get; }

        public static TestSqliteContextScope Create()
        {
            SqliteConnection connection = new("Data Source=:memory:");
            connection.Open();

            DbContextOptions<CloudFileDbContext> options = new DbContextOptionsBuilder<CloudFileDbContext>()
                .UseSqlite(connection)
                .Options;

            CloudFileDbContext dbContext = new(options);
            dbContext.Database.EnsureCreated();
            return new TestSqliteContextScope(connection, dbContext);
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }
    }

    private sealed class TestInMemoryContextScope : IDisposable
    {
        private TestInMemoryContextScope(CloudFileDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public CloudFileDbContext DbContext { get; }

        public static TestInMemoryContextScope Create()
        {
            DbContextOptions<CloudFileDbContext> options = new DbContextOptionsBuilder<CloudFileDbContext>()
                .UseInMemoryDatabase($"cfm-query-inmemory-{Guid.NewGuid():N}")
                .Options;

            CloudFileDbContext dbContext = new(options);
            return new TestInMemoryContextScope(dbContext);
        }

        public void Dispose()
        {
            DbContext.Dispose();
        }
    }
}
