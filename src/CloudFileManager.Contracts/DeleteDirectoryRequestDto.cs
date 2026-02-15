namespace CloudFileManager.Contracts;

/// <summary>
/// 刪除目錄請求DTO 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class DeleteDirectoryRequestDto
{
    /// <summary>
    /// 初始化 刪除目錄請求DTO。
    /// </summary>
    public DeleteDirectoryRequestDto()
    {
        DirectoryPath = string.Empty;
    }

    /// <summary>
    /// 初始化 刪除目錄請求DTO。
    /// </summary>
    public DeleteDirectoryRequestDto(string directoryPath)
    {
        DirectoryPath = directoryPath;
    }

    /// <summary>
    /// 取得或設定 目錄路徑。
    /// </summary>
    public string DirectoryPath { get; set; }
}
