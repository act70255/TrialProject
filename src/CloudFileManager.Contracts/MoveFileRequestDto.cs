namespace CloudFileManager.Contracts;

/// <summary>
/// 搬移檔案請求DTO 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class MoveFileRequestDto
{
    /// <summary>
    /// 初始化 搬移檔案請求DTO。
    /// </summary>
    public MoveFileRequestDto()
    {
        SourceFilePath = string.Empty;
        TargetDirectoryPath = string.Empty;
    }

    /// <summary>
    /// 初始化 搬移檔案請求DTO。
    /// </summary>
    public MoveFileRequestDto(string sourceFilePath, string targetDirectoryPath)
    {
        SourceFilePath = sourceFilePath;
        TargetDirectoryPath = targetDirectoryPath;
    }

    /// <summary>
    /// 取得或設定 Source檔案路徑。
    /// </summary>
    public string SourceFilePath { get; set; }

    /// <summary>
    /// 取得或設定 Target目錄路徑。
    /// </summary>
    public string TargetDirectoryPath { get; set; }
}
