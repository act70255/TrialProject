namespace CloudFileManager.Contracts;

/// <summary>
/// 檔案下載結果DTO 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class FileDownloadResultDto
{
    /// <summary>
    /// 初始化 檔案下載結果DTO。
    /// </summary>
    public FileDownloadResultDto()
    {
        Message = string.Empty;
        FileName = string.Empty;
        ContentType = "application/octet-stream";
    }

    /// <summary>
    /// 初始化 檔案下載結果DTO。
    /// </summary>
    public FileDownloadResultDto(bool success, string message, string fileName, byte[]? content, string contentType)
    {
        Success = success;
        Message = message;
        FileName = fileName;
        Content = content;
        ContentType = contentType;
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
    /// 取得或設定 檔案名稱。
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// 取得或設定 Content。
    /// </summary>
    public byte[]? Content { get; set; }

    /// <summary>
    /// 取得或設定 ContentType。
    /// </summary>
    public string ContentType { get; set; }
}
