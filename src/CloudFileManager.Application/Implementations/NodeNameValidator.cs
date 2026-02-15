namespace CloudFileManager.Application.Implementations;

/// <summary>
/// NodeNameValidator 類別，負責節點命名驗證。
/// </summary>
public static class NodeNameValidator
{
    /// <summary>
    /// 驗證節點名稱是否合法。
    /// </summary>
    public static string? Validate(string name, string nodeType)
    {
        string trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return $"{nodeType} name is required.";
        }

        if (trimmed.Contains('/', StringComparison.Ordinal) || trimmed.Contains('\\', StringComparison.Ordinal))
        {
            return $"{nodeType} name cannot contain path separators.";
        }

        if (string.Equals(trimmed, ".", StringComparison.Ordinal) || string.Equals(trimmed, "..", StringComparison.Ordinal))
        {
            return $"{nodeType} name is invalid.";
        }

        return null;
    }
}
