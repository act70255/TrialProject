namespace CloudFileManager.Presentation.WebApi.Model;

/// <summary>
/// 目錄樹 API 回應類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class DirectoryTreeApiResponse
{
    public DirectoryTreeApiResponse()
    {
        Lines = Array.Empty<string>();
    }

    public DirectoryTreeApiResponse(IReadOnlyList<string> lines)
    {
        Lines = lines;
    }

    public IReadOnlyList<string> Lines { get; set; }
}

/// <summary>
/// 操作結果 API 回應類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class OperationApiResponse
{
    public OperationApiResponse()
    {
        Message = string.Empty;
    }

    public OperationApiResponse(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public OperationApiResponse(bool success, string message, string? errorCode)
    {
        Success = success;
        Message = message;
        ErrorCode = errorCode;
    }

    public bool Success { get; set; }

    public string Message { get; set; }

    public string? ErrorCode { get; set; }
}

/// <summary>
/// 容量計算 API 回應類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class SizeCalculationApiResponse
{
    public SizeCalculationApiResponse()
    {
        IsFound = true;
        FormattedSize = string.Empty;
        TraverseLog = Array.Empty<string>();
    }

    public SizeCalculationApiResponse(bool isFound, long sizeBytes, string formattedSize, IReadOnlyList<string> traverseLog)
    {
        IsFound = isFound;
        SizeBytes = sizeBytes;
        FormattedSize = formattedSize;
        TraverseLog = traverseLog;
    }

    public bool IsFound { get; set; }

    public long SizeBytes { get; set; }

    public string FormattedSize { get; set; }

    public IReadOnlyList<string> TraverseLog { get; set; }
}

/// <summary>
/// 搜尋API回應 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class SearchApiResponse
{
    public SearchApiResponse()
    {
        Paths = Array.Empty<string>();
        TraverseLog = Array.Empty<string>();
    }

    public SearchApiResponse(IReadOnlyList<string> paths, IReadOnlyList<string> traverseLog)
    {
        Paths = paths;
        TraverseLog = traverseLog;
    }

    public IReadOnlyList<string> Paths { get; set; }

    public IReadOnlyList<string> TraverseLog { get; set; }
}

/// <summary>
/// XML 匯出 API 回應類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class XmlExportApiResponse
{
    public XmlExportApiResponse()
    {
        XmlContent = string.Empty;
    }

    public XmlExportApiResponse(string xmlContent, string? outputPath)
    {
        XmlContent = xmlContent;
        OutputPath = outputPath;
    }

    public string XmlContent { get; set; }

    public string? OutputPath { get; set; }
}

/// <summary>
/// 功能旗標API回應 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class FeatureFlagsApiResponse
{
    public FeatureFlagsApiResponse()
    {
        Flags = new Dictionary<string, bool>();
    }

    public FeatureFlagsApiResponse(IReadOnlyDictionary<string, bool> flags)
    {
        Flags = flags;
    }

    public IReadOnlyDictionary<string, bool> Flags { get; set; }
}
