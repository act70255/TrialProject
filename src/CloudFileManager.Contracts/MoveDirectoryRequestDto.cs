namespace CloudFileManager.Contracts;

/// <summary>
/// 搬移目錄請求DTO 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class MoveDirectoryRequestDto
{
    /// <summary>
    /// 初始化 搬移目錄請求DTO。
    /// </summary>
    public MoveDirectoryRequestDto()
    {
        SourceDirectoryPath = string.Empty;
        TargetParentDirectoryPath = string.Empty;
    }

    /// <summary>
    /// 初始化 搬移目錄請求DTO。
    /// </summary>
    public MoveDirectoryRequestDto(string sourceDirectoryPath, string targetParentDirectoryPath)
    {
        SourceDirectoryPath = sourceDirectoryPath;
        TargetParentDirectoryPath = targetParentDirectoryPath;
    }

    /// <summary>
    /// 取得或設定 Source目錄路徑。
    /// </summary>
    public string SourceDirectoryPath { get; set; }

    /// <summary>
    /// 取得或設定 TargetParent目錄路徑。
    /// </summary>
    public string TargetParentDirectoryPath { get; set; }
}
