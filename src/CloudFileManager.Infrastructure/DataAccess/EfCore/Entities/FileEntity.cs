namespace CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;

/// <summary>
/// 檔案Entity 類別，負責表示資料庫對應實體。
/// </summary>
public sealed class FileEntity
{
    /// <summary>
    /// 取得或設定 Id。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 取得或設定 目錄Id。
    /// </summary>
    public Guid DirectoryId { get; set; }

    /// <summary>
    /// 取得或設定 名稱。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 Extension。
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 容量Bytes。
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// 取得或設定 建立dTime。
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 取得或設定 檔案Type。
    /// </summary>
    public int FileType { get; set; }

    /// <summary>
    /// 取得或設定 CreationOrder。
    /// </summary>
    public int CreationOrder { get; set; }

    /// <summary>
    /// 取得或設定 Physical路徑。
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 目錄。
    /// </summary>
    public DirectoryEntity Directory { get; set; } = null!;

    /// <summary>
    /// 取得或設定 Metadata。
    /// </summary>
    public FileMetadataEntity Metadata { get; set; } = null!;

    /// <summary>
    /// 取得或設定節點標籤關聯。
    /// </summary>
    public List<NodeTagEntity> NodeTags { get; set; } = [];
}
