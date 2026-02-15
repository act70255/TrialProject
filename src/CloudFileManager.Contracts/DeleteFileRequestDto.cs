namespace CloudFileManager.Contracts;

/// <summary>
/// 刪除檔案請求DTO 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class DeleteFileRequestDto
{
    /// <summary>
    /// 初始化 刪除檔案請求DTO。
    /// </summary>
    public DeleteFileRequestDto()
    {
        FilePath = string.Empty;
    }

    /// <summary>
    /// 初始化 刪除檔案請求DTO。
    /// </summary>
    public DeleteFileRequestDto(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// 取得或設定 檔案路徑。
    /// </summary>
    public string FilePath { get; set; }
}
