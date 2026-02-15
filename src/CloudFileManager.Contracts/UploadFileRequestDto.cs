namespace CloudFileManager.Contracts;

/// <summary>
/// 上傳檔案請求DTO 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class UploadFileRequestDto
{
    /// <summary>
    /// 初始化 上傳檔案請求DTO。
    /// </summary>
    public UploadFileRequestDto()
    {
        DirectoryPath = string.Empty;
        FileName = string.Empty;
    }

    /// <summary>
    /// 初始化 上傳檔案請求DTO。
    /// </summary>
    public UploadFileRequestDto(
        string DirectoryPath,
        string FileName,
        long Size,
        int? PageCount = null,
        int? Width = null,
        int? Height = null,
        string? Encoding = null,
        string? SourceLocalPath = null)
    {
        this.DirectoryPath = DirectoryPath;
        this.FileName = FileName;
        this.Size = Size;
        this.PageCount = PageCount;
        this.Width = Width;
        this.Height = Height;
        this.Encoding = Encoding;
        this.SourceLocalPath = SourceLocalPath;
    }

    /// <summary>
    /// 取得或設定 目錄路徑。
    /// </summary>
    public string DirectoryPath { get; set; }

    /// <summary>
    /// 取得或設定 檔案名稱。
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// 取得或設定 容量。
    /// </summary>
    public long Size { get; set; }

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
    /// 取得或設定 SourceLocal路徑。
    /// </summary>
    public string? SourceLocalPath { get; set; }
}
