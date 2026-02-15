using CloudFileManager.Domain.Enums;

namespace CloudFileManager.Domain;

/// <summary>
/// CloudFile，定義檔案節點的共用欄位與行為。
/// </summary>
public abstract class CloudFile : FileSystemNode
{
    /// <summary>
    /// 建立檔案基底實例。
    /// </summary>
    ///<param name="name">檔名。</param>
    ///<param name="size">檔案大小（Bytes）。</param>
    ///<param name="createdTime">建立時間。</param>
    ///<param name="fileType">檔案型別。</param>
    protected CloudFile(string name, long size, DateTime createdTime, CloudFileType fileType)
        : base(name, createdTime)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);

        Size = size;
        FileType = fileType;
    }

    /// <summary>
    /// 取得檔案大小（Bytes）。
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// 取得檔案類型。
    /// </summary>
    public CloudFileType FileType { get; }

    /// <summary>
    /// 類型專屬細節文字（例如頁數、解析度、編碼）。
    /// </summary>
    public abstract string DetailText { get; }
}
