namespace CloudFileManager.Infrastructure.FileStorage;

/// <summary>
/// FileContentTypeResolver 類別，負責解析檔案內容類型。
/// </summary>
public static class FileContentTypeResolver
{
    /// <summary>
    /// 依副檔名解析 Content-Type。
    /// </summary>
    public static string Resolve(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".txt" => "text/plain",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }
}
