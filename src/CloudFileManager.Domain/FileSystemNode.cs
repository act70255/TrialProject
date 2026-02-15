namespace CloudFileManager.Domain;

/// <summary>
/// 檔案SystemNode 類別，負責處理檔案相關資料與行為。
/// </summary>
public abstract class FileSystemNode
{
    /// <summary>
    /// 初始化 檔案SystemNode。
    /// </summary>
    protected FileSystemNode(string name, DateTime createdTime)
    {
        Name = name;
        CreatedTime = createdTime;
    }

    /// <summary>
    /// 取得或設定名稱。
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// 取得建立時間。
    /// </summary>
    public DateTime CreatedTime { get; }

    /// <summary>
    /// 重新命名資料。
    /// </summary>
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Name is required.", nameof(newName));
        }

        Name = newName.Trim();
    }
}
