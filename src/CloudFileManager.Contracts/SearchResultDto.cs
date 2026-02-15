namespace CloudFileManager.Contracts;

/// <summary>
/// 搜尋結果DTO 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class SearchResultDto
{
    /// <summary>
    /// 初始化 搜尋結果DTO。
    /// </summary>
    public SearchResultDto()
    {
        Paths = Array.Empty<string>();
        TraverseLog = Array.Empty<string>();
    }

    /// <summary>
    /// 初始化 搜尋結果DTO。
    /// </summary>
    public SearchResultDto(IReadOnlyList<string> paths, IReadOnlyList<string> traverseLog)
    {
        Paths = paths;
        TraverseLog = traverseLog;
    }

    /// <summary>
    /// 取得或設定 路徑s。
    /// </summary>
    public IReadOnlyList<string> Paths { get; set; }

    /// <summary>
    /// 取得或設定 TraverseLog。
    /// </summary>
    public IReadOnlyList<string> TraverseLog { get; set; }
}
