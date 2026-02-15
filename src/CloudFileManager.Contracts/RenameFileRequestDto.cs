namespace CloudFileManager.Contracts;

/// <summary>
/// 重新命名檔案請求DTO 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class RenameFileRequestDto
{
    /// <summary>
    /// 初始化 重新命名檔案請求DTO。
    /// </summary>
    public RenameFileRequestDto()
    {
        FilePath = string.Empty;
        NewFileName = string.Empty;
    }

    /// <summary>
    /// 初始化 重新命名檔案請求DTO。
    /// </summary>
    public RenameFileRequestDto(string filePath, string newFileName)
    {
        FilePath = filePath;
        NewFileName = newFileName;
    }

    /// <summary>
    /// 取得或設定 檔案路徑。
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// 取得或設定 New檔案名稱。
    /// </summary>
    public string NewFileName { get; set; }
}
