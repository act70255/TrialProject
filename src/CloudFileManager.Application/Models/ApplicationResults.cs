namespace CloudFileManager.Application.Models;

public sealed record DirectoryTreeResult(IReadOnlyList<string> Lines, DirectoryNodeResult Root);

public sealed record DirectoryNodeResult(
    string Name,
    IReadOnlyList<DirectoryNodeResult> Directories,
    IReadOnlyList<FileNodeResult> Files);

public sealed record FileNodeResult(string Name);

public sealed record OperationResult(bool Success, string Message, string? ErrorCode = null);

public sealed record FileDownloadResult(
    bool Success,
    string Message,
    string FileName,
    byte[]? Content,
    string ContentType);

public sealed record SizeCalculationResult(
    bool IsFound,
    long SizeBytes,
    string FormattedSize,
    IReadOnlyList<string> TraverseLog);

public sealed record SearchResult(IReadOnlyList<string> Paths, IReadOnlyList<string> TraverseLog);

public sealed record XmlExportResult(string XmlContent, string? OutputPath);

public sealed record FeatureFlagsResult(IReadOnlyDictionary<string, bool> Flags);
