namespace CloudFileManager.Contracts;

/// <summary>
/// 建立目錄請求DTO 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class CreateDirectoryRequestDto
{
    /// <summary>
    /// 初始化 建立目錄請求DTO。
    /// </summary>
    public CreateDirectoryRequestDto()
    {
        ParentPath = string.Empty;
        DirectoryName = string.Empty;
    }

    /// <summary>
    /// 初始化 建立目錄請求DTO。
    /// </summary>
    public CreateDirectoryRequestDto(string parentPath, string directoryName)
    {
        ParentPath = parentPath;
        DirectoryName = directoryName;
    }

    /// <summary>
    /// 取得或設定 Parent路徑。
    /// </summary>
    public string ParentPath { get; set; }

    /// <summary>
    /// 取得或設定 目錄名稱。
    /// </summary>
    public string DirectoryName { get; set; }
}
