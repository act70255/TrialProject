using CloudFileManager.Infrastructure.DataAccess.EfCore;
using CloudFileManager.Infrastructure.FileStorage;

namespace CloudFileManager.UnitTests;

public sealed class InfrastructureUtilityTests
{
    [Theory]
    [InlineData("sample.txt", "text/plain")]
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("doc.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("unknown.bin", "application/octet-stream")]
    public void FileContentTypeResolver_ShouldResolveExpectedContentType(string fileName, string expected)
    {
        string actual = FileContentTypeResolver.Resolve(fileName);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CloudFileDbContextFactory_ShouldCreateSqliteDbContext()
    {
        CloudFileDbContextFactory factory = new();

        using CloudFileDbContext dbContext = factory.CreateDbContext([]);

        Assert.NotNull(dbContext);
        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", dbContext.Database.ProviderName);
    }
}
