namespace CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;

/// <summary>
/// TagEntity 類別，負責表示標籤主檔。
/// </summary>
public sealed class TagEntity
{
    /// <summary>
    /// 取得或設定 Id。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 取得或設定標籤名稱。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定標籤顏色。
    /// </summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定建立時間。
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 取得或設定節點標籤關聯。
    /// </summary>
    public List<NodeTagEntity> NodeTags { get; set; } = [];
}
