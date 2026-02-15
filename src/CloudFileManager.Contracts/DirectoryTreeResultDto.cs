namespace CloudFileManager.Contracts;

/// <summary>
/// 目錄目錄樹結果DTO 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class DirectoryTreeResultDto
{
    /// <summary>
    /// 初始化 目錄目錄樹結果DTO。
    /// </summary>
    public DirectoryTreeResultDto()
    {
        Lines = Array.Empty<string>();
    }

    /// <summary>
    /// 初始化 目錄目錄樹結果DTO。
    /// </summary>
    public DirectoryTreeResultDto(IReadOnlyList<string> lines)
    {
        Lines = lines;
    }

    /// <summary>
    /// 取得或設定 Lines。
    /// </summary>
    public IReadOnlyList<string> Lines { get; set; }
}
