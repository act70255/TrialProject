namespace CloudFileManager.Contracts;

/// <summary>
/// 重新命名目錄請求DTO 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class RenameDirectoryRequestDto
{
    /// <summary>
    /// 初始化 重新命名目錄請求DTO。
    /// </summary>
    public RenameDirectoryRequestDto()
    {
        DirectoryPath = string.Empty;
        NewDirectoryName = string.Empty;
    }

    /// <summary>
    /// 初始化 重新命名目錄請求DTO。
    /// </summary>
    public RenameDirectoryRequestDto(string directoryPath, string newDirectoryName)
    {
        DirectoryPath = directoryPath;
        NewDirectoryName = newDirectoryName;
    }

    /// <summary>
    /// 取得或設定 目錄路徑。
    /// </summary>
    public string DirectoryPath { get; set; }

    /// <summary>
    /// 取得或設定 New目錄名稱。
    /// </summary>
    public string NewDirectoryName { get; set; }
}
