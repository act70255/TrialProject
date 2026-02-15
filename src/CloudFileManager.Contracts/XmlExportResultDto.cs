namespace CloudFileManager.Contracts;

/// <summary>
/// XmlExportResultDto，封裝 XML 匯出結果 DTO。
/// </summary>
public sealed class XmlExportResultDto
{
    /// <summary>
    /// 初始化 XmlExportResultDto。
    /// </summary>
    public XmlExportResultDto()
    {
        XmlContent = string.Empty;
    }

    /// <summary>
    /// 初始化 XmlExportResultDto。
    /// </summary>
    public XmlExportResultDto(string xmlContent, string? outputPath)
    {
        XmlContent = xmlContent;
        OutputPath = outputPath;
    }

    /// <summary>
    /// 取得或設定 XMLContent。
    /// </summary>
    public string XmlContent { get; set; }

    /// <summary>
    /// 取得或設定 Output路徑。
    /// </summary>
    public string? OutputPath { get; set; }
}
