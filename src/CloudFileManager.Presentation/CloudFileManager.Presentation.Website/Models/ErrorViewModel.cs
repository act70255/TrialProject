namespace CloudFileManager.Presentation.Website.Models;

/// <summary>
/// 錯誤ViewModel 類別，負責封裝資料傳輸結構。
/// </summary>
public class ErrorViewModel
{
    /// <summary>
    /// 取得或設定 請求Id。
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// 是否顯示請求 Id。
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
