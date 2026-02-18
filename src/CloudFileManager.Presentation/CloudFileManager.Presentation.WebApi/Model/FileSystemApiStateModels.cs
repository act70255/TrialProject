using CloudFileManager.Application.Models;

namespace CloudFileManager.Presentation.WebApi.Model;

/// <summary>
/// 客戶端會話狀態模型。
/// </summary>
public sealed class ClientSessionStateApiModel
{
    public ClientSessionStateApiModel()
    {
        CurrentDirectoryPath = "Root";
        NodeTags = new Dictionary<string, IReadOnlyList<string>>();
        UndoStack = Array.Empty<SessionUndoActionApiModel>();
        RedoStack = Array.Empty<SessionUndoActionApiModel>();
    }

    public string CurrentDirectoryPath { get; set; }

    public SessionClipboardItemApiModel? ClipboardItem { get; set; }

    public SessionSortStateApiModel? CurrentSortState { get; set; }

    public Dictionary<string, IReadOnlyList<string>> NodeTags { get; set; }

    public IReadOnlyList<SessionUndoActionApiModel> UndoStack { get; set; }

    public IReadOnlyList<SessionUndoActionApiModel> RedoStack { get; set; }
}

/// <summary>
/// 會話剪貼簿模型。
/// </summary>
public sealed class SessionClipboardItemApiModel
{
    public SessionClipboardItemApiModel()
    {
        Path = string.Empty;
    }

    public SessionClipboardItemApiModel(bool isDirectory, string path)
    {
        IsDirectory = isDirectory;
        Path = path;
    }

    public bool IsDirectory { get; set; }

    public string Path { get; set; }
}

/// <summary>
/// 會話排序模型。
/// </summary>
public sealed class SessionSortStateApiModel
{
    public SessionSortStateApiModel()
    {
        Key = string.Empty;
        Direction = string.Empty;
    }

    public SessionSortStateApiModel(string key, string direction)
    {
        Key = key;
        Direction = direction;
    }

    public string Key { get; set; }

    public string Direction { get; set; }
}

/// <summary>
/// 會話 Undo 操作模型。
/// </summary>
public sealed class SessionUndoActionApiModel
{
    public SessionUndoActionApiModel()
    {
        Kind = string.Empty;
    }

    public string Kind { get; set; }

    public SessionSortStateApiModel? PreviousSortState { get; set; }

    public SessionSortStateApiModel? CurrentSortState { get; set; }

    public string? NodePath { get; set; }

    public string? TagName { get; set; }

    public string? TagColor { get; set; }
}

/// <summary>
/// 包含狀態與輸出內容的 API 回應模型。
/// </summary>
public sealed class StatefulApiResponse<TData>
{
    public StatefulApiResponse()
    {
        Message = string.Empty;
        OutputLines = Array.Empty<string>();
        State = new ClientSessionStateApiModel();
    }

    public StatefulApiResponse(bool success, string message, string? errorCode, TData? data, ClientSessionStateApiModel state, IReadOnlyList<string> outputLines)
    {
        Success = success;
        Message = message;
        ErrorCode = errorCode;
        Data = data;
        State = state;
        OutputLines = outputLines;
    }

    public bool Success { get; set; }

    public string Message { get; set; }

    public string? ErrorCode { get; set; }

    public TData? Data { get; set; }

    public ClientSessionStateApiModel State { get; set; }

    public IReadOnlyList<string> OutputLines { get; set; }
}

/// <summary>
/// 目錄項目 API 回應模型。
/// </summary>
public sealed class DirectoryEntryApiResponse
{
    public DirectoryEntryApiResponse()
    {
        Name = string.Empty;
        FullPath = string.Empty;
        FormattedSize = string.Empty;
        Extension = string.Empty;
    }

    public DirectoryEntryApiResponse(string name, bool isDirectory, string fullPath, long sizeBytes, string formattedSize, string extension, int siblingOrder)
    {
        Name = name;
        IsDirectory = isDirectory;
        FullPath = fullPath;
        SizeBytes = sizeBytes;
        FormattedSize = formattedSize;
        Extension = extension;
        SiblingOrder = siblingOrder;
    }

    public string Name { get; set; }

    public bool IsDirectory { get; set; }

    public string FullPath { get; set; }

    public long SizeBytes { get; set; }

    public string FormattedSize { get; set; }

    public string Extension { get; set; }

    public int SiblingOrder { get; set; }
}

/// <summary>
/// 目錄項目集合 API 回應模型。
/// </summary>
public sealed class DirectoryEntriesApiResponse
{
    public DirectoryEntriesApiResponse()
    {
        Entries = Array.Empty<DirectoryEntryApiResponse>();
    }

    public DirectoryEntriesApiResponse(bool isFound, IReadOnlyList<DirectoryEntryApiResponse> entries)
    {
        IsFound = isFound;
        Entries = entries;
    }

    public bool IsFound { get; set; }

    public IReadOnlyList<DirectoryEntryApiResponse> Entries { get; set; }
}

/// <summary>
/// 標籤節點回應模型。
/// </summary>
public sealed class TaggedNodeApiResponse
{
    public TaggedNodeApiResponse()
    {
        Path = string.Empty;
        Tags = Array.Empty<string>();
    }

    public TaggedNodeApiResponse(string path, IReadOnlyList<string> tags)
    {
        Path = path;
        Tags = tags;
    }

    public string Path { get; set; }

    public IReadOnlyList<string> Tags { get; set; }
}

/// <summary>
/// 標籤列表回應模型。
/// </summary>
public sealed class TagListApiResponse
{
    public TagListApiResponse()
    {
        Items = Array.Empty<TaggedNodeApiResponse>();
    }

    public TagListApiResponse(IReadOnlyList<TaggedNodeApiResponse> items)
    {
        Items = items;
    }

    public IReadOnlyList<TaggedNodeApiResponse> Items { get; set; }
}

/// <summary>
/// 標籤查詢回應模型。
/// </summary>
public sealed class TagFindResultApiResponse
{
    public TagFindResultApiResponse()
    {
        Tag = string.Empty;
        Color = string.Empty;
        ScopePath = string.Empty;
        Paths = Array.Empty<string>();
    }

    public TagFindResultApiResponse(string tag, string color, string scopePath, IReadOnlyList<string> paths)
    {
        Tag = tag;
        Color = color;
        ScopePath = scopePath;
        Paths = paths;
    }

    public string Tag { get; set; }

    public string Color { get; set; }

    public string ScopePath { get; set; }

    public IReadOnlyList<string> Paths { get; set; }
}

/// <summary>
/// 操作完成後的狀態資訊回應。
/// </summary>
public sealed class StateOperationInfoApiResponse
{
    public StateOperationInfoApiResponse()
    {
        Value = string.Empty;
    }

    public StateOperationInfoApiResponse(string value)
    {
        Value = value;
    }

    public string Value { get; set; }
}

/// <summary>
/// 內部會話命令結果模型。
/// </summary>
public sealed record SessionCommandResult<TData>(
    OperationResult Operation,
    ClientSessionStateApiModel State,
    IReadOnlyList<string> OutputLines,
    TData? Data = default);
