namespace CloudFileManager.Presentation.Website.Models;

/// <summary>
/// HomeIndexViewModel，封裝首頁操作結果與顯示資料。
/// </summary>
public sealed class HomeIndexViewModel
{
    /// <summary>
    /// 取得或設定目錄樹 Lines。
    /// </summary>
    public IReadOnlyList<string> TreeLines { get; set; } = [];

    /// <summary>
    /// 取得或設定錯誤訊息。
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 取得或設定 錯誤碼。
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 取得或設定操作是否成功。
    /// </summary>
    public bool? OperationSuccess { get; set; }

    /// <summary>
    /// 取得或設定操作訊息。
    /// </summary>
    public string? OperationMessage { get; set; }

    /// <summary>
    /// 取得或設定操作錯誤碼。
    /// </summary>
    public string? OperationErrorCode { get; set; }

    /// <summary>
    /// 取得或設定 容量路徑。
    /// </summary>
    public string? SizePath { get; set; }

    /// <summary>
    /// 取得或設定格式化容量字串。
    /// </summary>
    public string? SizeFormatted { get; set; }

    /// <summary>
    /// 取得或設定容量計算 TraverseLog。
    /// </summary>
    public IReadOnlyList<string> SizeTraverseLog { get; set; } = [];

    /// <summary>
    /// 取得或設定搜尋副檔名。
    /// </summary>
    public string? SearchExtension { get; set; }

    /// <summary>
    /// 取得或設定搜尋路徑清單。
    /// </summary>
    public IReadOnlyList<string> SearchPaths { get; set; } = [];

    /// <summary>
    /// 取得或設定搜尋 TraverseLog。
    /// </summary>
    public IReadOnlyList<string> SearchTraverseLog { get; set; } = [];

    /// <summary>
    /// 取得或設定 XMLContent。
    /// </summary>
    public string? XmlContent { get; set; }

    /// <summary>
    /// 取得或設定 XML 輸出路徑。
    /// </summary>
    public string? XmlOutputPath { get; set; }

    public IReadOnlyDictionary<string, bool> FeatureFlags { get; set; } = new Dictionary<string, bool>();
}
