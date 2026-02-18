namespace CloudFileManager.Application.Models;

public sealed record CreateDirectoryRequest(string ParentPath, string DirectoryName);

public sealed record UploadFileRequest(
    string DirectoryPath,
    string FileName,
    long Size,
    int? PageCount = null,
    int? Width = null,
    int? Height = null,
    string? Encoding = null,
    string? SourceLocalPath = null);

public sealed record MoveFileRequest(string SourceFilePath, string TargetDirectoryPath);

public sealed record RenameFileRequest(string FilePath, string NewFileName);

public sealed record DeleteFileRequest(string FilePath);

public sealed record DownloadFileRequest(string FilePath, string TargetLocalPath);

public sealed record DeleteDirectoryRequest(string DirectoryPath);

public sealed record MoveDirectoryRequest(string SourceDirectoryPath, string TargetParentDirectoryPath);

public sealed record RenameDirectoryRequest(string DirectoryPath, string NewDirectoryName);

public sealed record CalculateSizeRequest(string DirectoryPath);

public sealed record SearchByExtensionRequest(string Extension, string? DirectoryPath = null);

public sealed record ExportXmlRequest(string? DirectoryPath = null);

public sealed record ListDirectoryEntriesRequest(string DirectoryPath);

public sealed record CopyFileRequest(string SourceFilePath, string TargetDirectoryPath, string? NewFileName = null);

public sealed record CopyDirectoryRequest(string SourceDirectoryPath, string TargetParentDirectoryPath, string? NewDirectoryName = null);

public sealed record AssignTagRequest(string Path, string Tag);

public sealed record RemoveTagRequest(string Path, string Tag);

public sealed record ListTagsRequest(string? Path = null);

public sealed record FindTagsRequest(string Tag, string? DirectoryPath = null);
