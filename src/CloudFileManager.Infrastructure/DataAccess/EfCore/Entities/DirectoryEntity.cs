namespace CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;

/// <summary>
/// 目錄Entity 類別，負責表示資料庫對應實體。
/// </summary>
public sealed class DirectoryEntity
{
    /// <summary>
    /// 取得或設定 Id。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 取得或設定 ParentId。
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 取得或設定 名稱。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 建立dTime。
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 取得或設定 CreationOrder。
    /// </summary>
    public int CreationOrder { get; set; }

    /// <summary>
    /// 取得或設定 Physical路徑。
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 Parent。
    /// </summary>
    public DirectoryEntity? Parent { get; set; }

    /// <summary>
    /// 取得或設定 Children。
    /// </summary>
    public List<DirectoryEntity> Children { get; set; } = new();

    /// <summary>
    /// 取得或設定 檔案s。
    /// </summary>
    public List<FileEntity> Files { get; set; } = new();

    /// <summary>
    /// 取得或設定節點標籤關聯。
    /// </summary>
    public List<NodeTagEntity> NodeTags { get; set; } = [];
}
