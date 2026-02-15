namespace CloudFileManager.Contracts;

/// <summary>
/// 計算容量請求DTO 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class CalculateSizeRequestDto
{
    /// <summary>
    /// 初始化 計算容量請求DTO。
    /// </summary>
    public CalculateSizeRequestDto()
    {
        DirectoryPath = string.Empty;
    }

    /// <summary>
    /// 初始化 計算容量請求DTO。
    /// </summary>
    public CalculateSizeRequestDto(string directoryPath)
    {
        DirectoryPath = directoryPath;
    }

    /// <summary>
    /// 取得或設定 目錄路徑。
    /// </summary>
    public string DirectoryPath { get; set; }
}
