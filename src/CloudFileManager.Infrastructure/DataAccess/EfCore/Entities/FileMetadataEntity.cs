namespace CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;

/// <summary>
/// 檔案MetadataEntity 類別，負責表示資料庫對應實體。
/// </summary>
public sealed class FileMetadataEntity
{
    /// <summary>
    /// 取得或設定 檔案Id。
    /// </summary>
    public Guid FileId { get; set; }

    /// <summary>
    /// 取得或設定 檔案Type。
    /// </summary>
    public int FileType { get; set; }

    /// <summary>
    /// 取得或設定 PageCount。
    /// </summary>
    public int? PageCount { get; set; }

    /// <summary>
    /// 取得或設定 Width。
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// 取得或設定 Height。
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// 取得或設定 Encoding。
    /// </summary>
    public string? Encoding { get; set; }

    /// <summary>
    /// 取得或設定 檔案。
    /// </summary>
    public FileEntity File { get; set; } = null!;
}
