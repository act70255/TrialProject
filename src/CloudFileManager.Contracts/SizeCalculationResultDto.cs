namespace CloudFileManager.Contracts;

/// <summary>
/// SizeCalculationResultDto，封裝容量計算結果 DTO。
/// </summary>
public sealed class SizeCalculationResultDto
{
    /// <summary>
    /// 初始化 SizeCalculationResultDto。
    /// </summary>
    public SizeCalculationResultDto()
    {
        IsFound = true;
        FormattedSize = string.Empty;
        TraverseLog = Array.Empty<string>();
    }

    /// <summary>
    /// 初始化 SizeCalculationResultDto。
    /// </summary>
    public SizeCalculationResultDto(long sizeBytes, string formattedSize, IReadOnlyList<string> traverseLog)
    {
        IsFound = true;
        SizeBytes = sizeBytes;
        FormattedSize = formattedSize;
        TraverseLog = traverseLog;
    }

    /// <summary>
    /// 初始化 SizeCalculationResultDto。
    /// </summary>
    public SizeCalculationResultDto(bool isFound, long sizeBytes, string formattedSize, IReadOnlyList<string> traverseLog)
    {
        IsFound = isFound;
        SizeBytes = sizeBytes;
        FormattedSize = formattedSize;
        TraverseLog = traverseLog;
    }

    /// <summary>
    /// 取得或設定 是否找到目錄。
    /// </summary>
    public bool IsFound { get; set; }

    /// <summary>
    /// 取得或設定 容量Bytes。
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// 取得或設定 Formatted容量。
    /// </summary>
    public string FormattedSize { get; set; }

    /// <summary>
    /// 取得或設定 TraverseLog。
    /// </summary>
    public IReadOnlyList<string> TraverseLog { get; set; }
}
