using CloudFileManager.Shared.Common;

namespace CloudFileManager.UnitTests;

/// <summary>
/// PathHierarchyRulesTests 類別，負責驗證路徑階層判斷規則。
/// </summary>
public sealed class PathHierarchyRulesTests
{
    [Theory]
    [InlineData("Root/A", "Root/A", true)]
    [InlineData("Root/A", "Root/A/B", true)]
    [InlineData("Root/A", "Root/AB", false)]
    [InlineData("Root/A", "Root/B", false)]
    [InlineData("Root/A", "Root", false)]
    [InlineData("Root/A", "root/a/b", true)]
    public void IsSameOrDescendant_ShouldMatchSegmentBoundaries(string sourcePath, string targetPath, bool expected)
    {
        bool actual = PathHierarchyRules.IsSameOrDescendant(sourcePath, targetPath);

        Assert.Equal(expected, actual);
    }
}
