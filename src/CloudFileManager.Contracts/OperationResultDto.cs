namespace CloudFileManager.Contracts;

/// <summary>
/// OperationResultDto，封裝操作結果 DTO。
/// </summary>
public sealed class OperationResultDto
{
    /// <summary>
    /// 初始化 OperationResultDto。
    /// </summary>
    public OperationResultDto()
    {
        Message = string.Empty;
    }

    /// <summary>
    /// 初始化 OperationResultDto。
    /// </summary>
    public OperationResultDto(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    /// <summary>
    /// 初始化 OperationResultDto。
    /// </summary>
    public OperationResultDto(bool success, string message, string? errorCode)
    {
        Success = success;
        Message = message;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// 取得或設定 Success。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 取得或設定 Message。
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// 取得或設定錯誤碼。
    /// </summary>
    public string? ErrorCode { get; set; }
}
