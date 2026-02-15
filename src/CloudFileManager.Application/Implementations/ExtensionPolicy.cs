using CloudFileManager.Application.Configuration;
using CloudFileManager.Domain.Enums;

namespace CloudFileManager.Application.Implementations;

/// <summary>
/// ExtensionPolicy 類別，負責封裝規則與驗證邏輯。
/// </summary>
public sealed class ExtensionPolicy
{
    private readonly Dictionary<CloudFileType, HashSet<string>> _allowedByType;

    /// <summary>
    /// 初始化 ExtensionPolicy。
    /// </summary>
    public ExtensionPolicy(AppConfig config)
    {
        _allowedByType = new Dictionary<CloudFileType, HashSet<string>>
        {
            [CloudFileType.Word] = new HashSet<string>(config.AllowedExtensions.Word.Select(ConfigDefaults.NormalizeExtension), StringComparer.OrdinalIgnoreCase),
            [CloudFileType.Image] = new HashSet<string>(config.AllowedExtensions.Image.Select(ConfigDefaults.NormalizeExtension), StringComparer.OrdinalIgnoreCase),
            [CloudFileType.Text] = new HashSet<string>(config.AllowedExtensions.Text.Select(ConfigDefaults.NormalizeExtension), StringComparer.OrdinalIgnoreCase)
        };
    }

    /// <summary>
    /// 判斷指定檔案類型是否允許該副檔名。
    /// </summary>
    public bool IsAllowed(CloudFileType fileType, string extension)
    {
        string normalized = ConfigDefaults.NormalizeExtension(extension);
        return _allowedByType.TryGetValue(fileType, out HashSet<string>? allowed) && allowed.Contains(normalized);
    }

    /// <summary>
    /// 判斷副檔名是否符合任一允許規則。
    /// </summary>
    public bool IsAllowedAny(string extension)
    {
        string normalized = ConfigDefaults.NormalizeExtension(extension);
        return _allowedByType.Values.Any(item => item.Contains(normalized));
    }
}
