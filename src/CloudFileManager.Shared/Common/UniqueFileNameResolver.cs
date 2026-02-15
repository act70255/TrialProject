namespace CloudFileManager.Shared.Common;

/// <summary>
/// UniqueFileNameResolver 類別，負責產生可用的唯一檔案名稱。
/// </summary>
public static class UniqueFileNameResolver
{
    /// <summary>
    /// 依既有名稱判斷器產生可用檔名。
    /// </summary>
    public static string Resolve(string fileName, Func<string, bool> exists)
    {
        string extension = Path.GetExtension(fileName);
        string baseName = Path.GetFileNameWithoutExtension(fileName);
        int index = 1;
        string candidate = fileName;

        while (exists(candidate))
        {
            candidate = $"{baseName}({index}){extension}";
            index++;
        }

        return candidate;
    }
}
