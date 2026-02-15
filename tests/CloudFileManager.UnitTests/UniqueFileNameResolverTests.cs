using CloudFileManager.Shared.Common;

namespace CloudFileManager.UnitTests;

/// <summary>
/// UniqueFileNameResolverTests 類別，負責驗證唯一檔名產生規則。
/// </summary>
public sealed class UniqueFileNameResolverTests
{
    [Fact]
    public void Resolve_ShouldReturnOriginalNameWhenNoConflict()
    {
        string result = UniqueFileNameResolver.Resolve("report.txt", _ => false);

        Assert.Equal("report.txt", result);
    }

    [Fact]
    public void Resolve_ShouldAppendSequenceWhenConflictExists()
    {
        HashSet<string> existing = new(StringComparer.OrdinalIgnoreCase)
        {
            "report.txt",
            "report(1).txt",
            "report(2).txt"
        };

        string result = UniqueFileNameResolver.Resolve("report.txt", candidate => existing.Contains(candidate));

        Assert.Equal("report(3).txt", result);
    }
}
