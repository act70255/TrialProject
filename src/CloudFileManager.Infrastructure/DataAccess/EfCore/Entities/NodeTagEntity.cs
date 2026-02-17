namespace CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;

/// <summary>
/// NodeTagEntity 類別，負責表示節點與標籤關聯。
/// </summary>
public sealed class NodeTagEntity
{
    /// <summary>
    /// 取得或設定 Id。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 取得或設定 TagId。
    /// </summary>
    public Guid TagId { get; set; }

    /// <summary>
    /// 取得或設定 DirectoryId。
    /// </summary>
    public Guid? DirectoryId { get; set; }

    /// <summary>
    /// 取得或設定 FileId。
    /// </summary>
    public Guid? FileId { get; set; }

    /// <summary>
    /// 取得或設定建立時間。
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 取得或設定 Tag。
    /// </summary>
    public TagEntity Tag { get; set; } = null!;

    /// <summary>
    /// 取得或設定目錄節點。
    /// </summary>
    public DirectoryEntity? Directory { get; set; }

    /// <summary>
    /// 取得或設定檔案節點。
    /// </summary>
    public FileEntity? File { get; set; }
}
