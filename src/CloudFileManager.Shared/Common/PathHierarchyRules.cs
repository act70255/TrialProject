namespace CloudFileManager.Shared.Common;

/// <summary>
/// PathHierarchyRules 類別，負責提供路徑階層判斷規則。
/// </summary>
public static class PathHierarchyRules
{
    /// <summary>
    /// 判斷 targetPath 是否為 sourcePath 本身或其子孫路徑。
    /// </summary>
    public static bool IsSameOrDescendant(string sourcePath, string targetPath)
    {
        string[] sourceSegments = SplitSegments(sourcePath);
        string[] targetSegments = SplitSegments(targetPath);

        if (sourceSegments.Length == 0 || targetSegments.Length == 0)
        {
            return false;
        }

        if (targetSegments.Length < sourceSegments.Length)
        {
            return false;
        }

        for (int index = 0; index < sourceSegments.Length; index++)
        {
            if (!string.Equals(sourceSegments[index], targetSegments[index], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static string[] SplitSegments(string path)
    {
        return path
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
