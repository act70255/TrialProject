namespace CloudFileManager.Application.Configuration;

/// <summary>
/// ConfigValidationError，封裝設定驗證錯誤資訊。
/// </summary>
public sealed class ConfigValidationError
{
    /// <summary>
    /// 初始化 ConfigValidationError。
    /// </summary>
    public ConfigValidationError()
    {
        ErrorCode = string.Empty;
        Field = string.Empty;
        Message = string.Empty;
    }

    /// <summary>
    /// 初始化 ConfigValidationError。
    /// </summary>
    public ConfigValidationError(string errorCode, string field, string message)
    {
        ErrorCode = errorCode;
        Field = field;
        Message = message;
    }

    /// <summary>
    /// 取得或設定錯誤代碼。
    /// </summary>
    public string ErrorCode { get; set; }

    /// <summary>
    /// 取得或設定欄位名稱。
    /// </summary>
    public string Field { get; set; }

    /// <summary>
    /// 取得或設定訊息內容。
    /// </summary>
    public string Message { get; set; }
}
