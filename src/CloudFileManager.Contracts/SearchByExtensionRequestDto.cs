namespace CloudFileManager.Contracts;

/// <summary>
/// 搜尋ByExtension請求DTO 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class SearchByExtensionRequestDto
{
    /// <summary>
    /// 初始化 搜尋ByExtension請求DTO。
    /// </summary>
    public SearchByExtensionRequestDto()
    {
        Extension = string.Empty;
    }

    /// <summary>
    /// 初始化 搜尋ByExtension請求DTO。
    /// </summary>
    public SearchByExtensionRequestDto(string extension)
    {
        Extension = extension;
    }

    /// <summary>
    /// 取得或設定 Extension。
    /// </summary>
    public string Extension { get; set; }
}
