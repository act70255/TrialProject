using CloudFileManager.Application.Interfaces;
using CloudFileManager.Application.Models;
using CloudFileManager.Presentation.WebApi.Model;
using CloudFileManager.Shared.Common;

namespace CloudFileManager.Presentation.WebApi.Services;

/// <summary>
/// Web API 狀態處理器，負責根據客戶端傳入狀態執行對齊 Console 的流程。
/// </summary>
public sealed class WebApiSessionStateProcessor
{
    private static readonly Dictionary<string, string> SupportedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Urgent"] = "Red",
        ["Work"] = "Blue",
        ["Personal"] = "Green"
    };

    private readonly ICloudFileApplicationService _service;

    public WebApiSessionStateProcessor(ICloudFileApplicationService service)
    {
        _service = service;
    }

    public SessionCommandResult<DirectoryEntriesApiResponse> GetDirectoryEntries(StatefulDirectoryEntriesApiRequest request)
    {
        RuntimeSessionState state = RuntimeSessionState.FromApi(request.State);
        RefreshStateTags(state);
        string directoryPath = string.IsNullOrWhiteSpace(request.DirectoryPath)
            ? state.CurrentDirectoryPath
            : ResolvePath(request.DirectoryPath, state.CurrentDirectoryPath);

        DirectoryEntriesResult result = _service.GetDirectoryEntries(new ListDirectoryEntriesRequest(directoryPath));
        if (!result.IsFound)
        {
            return Fail<DirectoryEntriesApiResponse>(state, $"Directory not found: {directoryPath}", OperationErrorCodes.ResourceNotFound);
        }

        IEnumerable<DirectoryEntryResult> entries = state.CurrentSortState is null
            ? result.Entries
            : ApplySortEntries(result.Entries, state.CurrentSortState.Key, state.CurrentSortState.Direction);

        List<string> outputLines = BuildDirectoryEntryOutputLines(directoryPath, entries, state.CurrentSortState);
        DirectoryEntriesApiResponse data = new(true, entries.Select(entry => entry.ToApi()).ToArray());
        return Success(state, "Directory entries listed.", outputLines, data);
    }

    public SessionCommandResult<StateOperationInfoApiResponse> ChangeCurrentDirectory(ChangeCurrentDirectoryApiRequest request)
    {
        RuntimeSessionState state = RuntimeSessionState.FromApi(request.State);
        string targetPath = ResolvePath(request.DirectoryPath, state.CurrentDirectoryPath);

        DirectorySnapshot snapshot = BuildSnapshot();
        if (!snapshot.DirectoryPaths.Contains(targetPath))
        {
            return Fail<StateOperationInfoApiResponse>(state, $"Directory not found: {targetPath}", OperationErrorCodes.ResourceNotFound);
        }

        state.CurrentDirectoryPath = targetPath;
        return Success(state, $"Current directory changed: {targetPath}", [$"[RESULT] Current directory: {targetPath}"], new StateOperationInfoApiResponse(targetPath));
    }

    public SessionCommandResult<StateOperationInfoApiResponse> SetSort(SetSortApiRequest request)
    {
        RuntimeSessionState state = RuntimeSessionState.FromApi(request.State);
        string key = request.Key.ToLowerInvariant();
        string direction = request.Direction.ToLowerInvariant();
        if (key is not ("name" or "size" or "ext"))
        {
            return Fail<StateOperationInfoApiResponse>(state, $"Unsupported sort key: {request.Key}", OperationErrorCodes.ValidationFailed);
        }

        if (direction is not ("asc" or "desc"))
        {
            return Fail<StateOperationInfoApiResponse>(state, $"Unsupported sort direction: {request.Direction}", OperationErrorCodes.ValidationFailed);
        }

        SessionSortStateApiModel? previousSortState = state.CurrentSortState;
        SessionSortStateApiModel currentSortState = new(key, direction);
        state.CurrentSortState = currentSortState;
        if (!AreSortStatesEqual(previousSortState, currentSortState))
        {
            state.RecordUndoAction(new SessionUndoActionApiModel
            {
                Kind = SessionUndoActionKinds.SortSettingChanged,
                PreviousSortState = CloneSortState(previousSortState),
                CurrentSortState = CloneSortState(currentSortState)
            });
        }

        return Success(state, $"Sort setting saved: {key} {direction}", [$"[RESULT] Sort: {key} {direction}"], new StateOperationInfoApiResponse($"{key}:{direction}"));
    }

    public SessionCommandResult<StateOperationInfoApiResponse> CopyToClipboard(ClipboardCopyApiRequest request)
    {
        RuntimeSessionState state = RuntimeSessionState.FromApi(request.State);
        string sourcePath = ResolvePath(request.SourcePath, state.CurrentDirectoryPath);
        DirectorySnapshot snapshot = BuildSnapshot();
        if (snapshot.DirectoryPaths.Contains(sourcePath))
        {
            state.ClipboardItem = new SessionClipboardItemApiModel(true, sourcePath);
            return Success(state, $"Copied directory: {sourcePath}", [$"[RESULT] Copied directory: {sourcePath}"], new StateOperationInfoApiResponse(sourcePath));
        }

        if (TryFindFilePath(snapshot, sourcePath, out string normalizedFilePath))
        {
            state.ClipboardItem = new SessionClipboardItemApiModel(false, normalizedFilePath);
            return Success(state, $"Copied file: {normalizedFilePath}", [$"[RESULT] Copied file: {normalizedFilePath}"], new StateOperationInfoApiResponse(normalizedFilePath));
        }

        return Fail<StateOperationInfoApiResponse>(state, $"Source not found: {sourcePath}", OperationErrorCodes.ResourceNotFound);
    }

    public SessionCommandResult<StateOperationInfoApiResponse> PasteFromClipboard(ClipboardPasteApiRequest request)
    {
        RuntimeSessionState state = RuntimeSessionState.FromApi(request.State);
        if (state.ClipboardItem is null)
        {
            return Fail<StateOperationInfoApiResponse>(state, "Clipboard is empty. Use copy first.", OperationErrorCodes.ValidationFailed);
        }

        string targetDirectoryPath = string.IsNullOrWhiteSpace(request.TargetDirectoryPath)
            ? state.CurrentDirectoryPath
            : ResolvePath(request.TargetDirectoryPath, state.CurrentDirectoryPath);
        OperationResult result = state.ClipboardItem.IsDirectory
            ? _service.CopyDirectory(new CopyDirectoryRequest(state.ClipboardItem.Path, targetDirectoryPath))
            : _service.CopyFile(new CopyFileRequest(state.ClipboardItem.Path, targetDirectoryPath));

        if (result.Success)
        {
            string sourcePath = state.ClipboardItem.Path.Replace('\\', '/').Trim().TrimEnd('/');
            string sourceName = sourcePath[(sourcePath.LastIndexOf('/') + 1)..];
            string targetPath = $"{targetDirectoryPath.TrimEnd('/')}/{sourceName}";
            DuplicateTagEntriesForCopy(state, sourcePath, targetPath);
        }

        return FromOperation(state, result, [result.Success ? $"[RESULT] OK: {result.Message}" : $"[RESULT] FAIL: {result.Message}"], new StateOperationInfoApiResponse(targetDirectoryPath));
    }

    public SessionCommandResult<StateOperationInfoApiResponse> AssignTag(TagAssignApiRequest request)
    {
        RuntimeSessionState state = RuntimeSessionState.FromApi(request.State);
        DirectorySnapshot snapshot = BuildSnapshot();

        string path = ResolvePath(request.Path, state.CurrentDirectoryPath);
        if (!TryResolveNodePath(snapshot, path, out string normalizedPath))
        {
            return Fail<StateOperationInfoApiResponse>(state, $"Node not found: {path}", OperationErrorCodes.ResourceNotFound);
        }

        if (!TryNormalizeTag(request.Tag, out string tagName, out string color))
        {
            return Fail<StateOperationInfoApiResponse>(state, $"Unsupported tag: {request.Tag}", OperationErrorCodes.ValidationFailed);
        }

        OperationResult operation = _service.AssignTag(new AssignTagRequest(normalizedPath, tagName));
        if (!operation.Success)
        {
            return FromOperation(state, operation, [$"[ERROR] {operation.Message}"], new StateOperationInfoApiResponse(normalizedPath));
        }

        RefreshStateTags(state);
        bool added = operation.Message.StartsWith("Tag assigned:", StringComparison.OrdinalIgnoreCase);
        if (added)
        {
            state.RecordUndoAction(new SessionUndoActionApiModel
            {
                Kind = SessionUndoActionKinds.TagAdded,
                NodePath = normalizedPath,
                TagName = tagName,
                TagColor = color
            });
        }

        string message = operation.Message;
        return Success(state, message, [$"[RESULT] {message}"], new StateOperationInfoApiResponse(normalizedPath));
    }

    public SessionCommandResult<StateOperationInfoApiResponse> RemoveTag(TagRemoveApiRequest request)
    {
        RuntimeSessionState state = RuntimeSessionState.FromApi(request.State);
        DirectorySnapshot snapshot = BuildSnapshot();

        string path = ResolvePath(request.Path, state.CurrentDirectoryPath);
        if (!TryResolveNodePath(snapshot, path, out string normalizedPath))
        {
            return Fail<StateOperationInfoApiResponse>(state, $"Node not found: {path}", OperationErrorCodes.ResourceNotFound);
        }

        if (!TryNormalizeTag(request.Tag, out string tagName, out string _))
        {
            return Fail<StateOperationInfoApiResponse>(state, $"Unsupported tag: {request.Tag}", OperationErrorCodes.ValidationFailed);
        }

        OperationResult operation = _service.RemoveTag(new RemoveTagRequest(normalizedPath, tagName));
        if (!operation.Success)
        {
            return FromOperation(state, operation, [$"[ERROR] {operation.Message}"], new StateOperationInfoApiResponse(normalizedPath));
        }

        RefreshStateTags(state);

        state.RecordUndoAction(new SessionUndoActionApiModel
        {
            Kind = SessionUndoActionKinds.TagRemoved,
            NodePath = normalizedPath,
            TagName = tagName,
            TagColor = SupportedTags[tagName]
        });

        return Success(state, operation.Message, [$"[RESULT] {operation.Message}"], new StateOperationInfoApiResponse(normalizedPath));
    }

    public SessionCommandResult<TagListApiResponse> ListTags(TagListApiRequest request)
    {
        RuntimeSessionState state = RuntimeSessionState.FromApi(request.State);
        DirectorySnapshot snapshot = BuildSnapshot();

        if (string.IsNullOrWhiteSpace(request.Path))
        {
            TagListResult allTagResult = _service.ListTags(new ListTagsRequest());
            RefreshStateTags(state, allTagResult);
            List<TaggedNodeApiResponse> allItems = allTagResult.Items
                .Select(item => new TaggedNodeApiResponse(item.Path, item.Tags.Select(tag => tag.Name).ToArray()))
                .ToList();
            return Success(state, "Tag list loaded.", BuildTagListOutputLines(allItems), new TagListApiResponse(allItems));
        }

        string path = ResolvePath(request.Path, state.CurrentDirectoryPath);
        if (!TryResolveNodePath(snapshot, path, out string normalizedPath))
        {
            return Fail<TagListApiResponse>(state, $"Node not found: {path}", OperationErrorCodes.ResourceNotFound);
        }

        TagListResult scopedTagResult = _service.ListTags(new ListTagsRequest(normalizedPath));
        RefreshStateTags(state, scopedTagResult);
        if (scopedTagResult.Items.Count == 0)
        {
            return Success(state, $"No tags on node: {normalizedPath}", [$"[INFO] No tags on node: {normalizedPath}"], new TagListApiResponse(Array.Empty<TaggedNodeApiResponse>()));
        }

        List<TaggedNodeApiResponse> itemList = scopedTagResult.Items
            .Select(item => new TaggedNodeApiResponse(item.Path, item.Tags.Select(tag => tag.Name).ToArray()))
            .ToList();
        return Success(state, "Tag list loaded.", BuildTagListOutputLines(itemList), new TagListApiResponse(itemList));
    }

    public SessionCommandResult<TagFindResultApiResponse> FindTags(TagFindApiRequest request)
    {
        RuntimeSessionState state = RuntimeSessionState.FromApi(request.State);
        if (!TryNormalizeTag(request.Tag, out string tagName, out string _))
        {
            return Fail<TagFindResultApiResponse>(state, $"Unsupported tag: {request.Tag}", OperationErrorCodes.ValidationFailed);
        }

        string scopePath = string.IsNullOrWhiteSpace(request.DirectoryPath)
            ? state.CurrentDirectoryPath
            : ResolvePath(request.DirectoryPath, state.CurrentDirectoryPath);
        DirectorySnapshot snapshot = BuildSnapshot();
        if (!snapshot.DirectoryPaths.Contains(scopePath))
        {
            return Fail<TagFindResultApiResponse>(state, $"Directory not found: {scopePath}", OperationErrorCodes.ResourceNotFound);
        }

        TagFindResult result = _service.FindTags(new FindTagsRequest(tagName, scopePath));
        TagFindResultApiResponse data = new(result.Tag, result.Color, result.ScopePath, result.Paths);
        return Success(state, "Tag query completed.", BuildTagFindOutputLines(data), data);
    }

    public SessionCommandResult<StateOperationInfoApiResponse> Undo(HistoryActionApiRequest request)
    {
        RuntimeSessionState state = RuntimeSessionState.FromApi(request.State);
        if (state.UndoStack.Count == 0)
        {
            return Fail<StateOperationInfoApiResponse>(state, "Nothing to undo.", OperationErrorCodes.ValidationFailed);
        }

        SessionUndoActionApiModel action = state.UndoStack.Pop();
        OperationResult operation = ApplyUndoAction(state, action, isRedo: false);
        if (!operation.Success)
        {
            state.UndoStack.Push(action);
            return FromOperation(state, operation, [$"[ERROR] {operation.Message}"], new StateOperationInfoApiResponse(action.Kind));
        }

        state.RedoStack.Push(action);
        return Success(state, $"Undo: {action.Kind}", [$"[RESULT] Undo: {action.Kind}"], new StateOperationInfoApiResponse(action.Kind));
    }

    public SessionCommandResult<StateOperationInfoApiResponse> Redo(HistoryActionApiRequest request)
    {
        RuntimeSessionState state = RuntimeSessionState.FromApi(request.State);
        if (state.RedoStack.Count == 0)
        {
            return Fail<StateOperationInfoApiResponse>(state, "Nothing to redo.", OperationErrorCodes.ValidationFailed);
        }

        SessionUndoActionApiModel action = state.RedoStack.Pop();
        OperationResult operation = ApplyUndoAction(state, action, isRedo: true);
        if (!operation.Success)
        {
            state.RedoStack.Push(action);
            return FromOperation(state, operation, [$"[ERROR] {operation.Message}"], new StateOperationInfoApiResponse(action.Kind));
        }

        state.UndoStack.Push(action);
        return Success(state, $"Redo: {action.Kind}", [$"[RESULT] Redo: {action.Kind}"], new StateOperationInfoApiResponse(action.Kind));
    }

    public SessionCommandResult<SearchApiResponse> Search(StatefulSearchApiRequest request)
    {
        RuntimeSessionState state = RuntimeSessionState.FromApi(request.State);
        string directoryPath = string.IsNullOrWhiteSpace(request.DirectoryPath)
            ? state.CurrentDirectoryPath
            : ResolvePath(request.DirectoryPath, state.CurrentDirectoryPath);
        SearchResult result = _service.SearchByExtension(new SearchByExtensionRequest(request.Extension, directoryPath));
        SearchApiResponse data = result.ToApi();
        return Success(state, "Search completed.", BuildSearchOutputLines(data), data);
    }

    public SessionCommandResult<XmlExportApiResponse> ExportXml(StatefulXmlExportApiRequest request)
    {
        RuntimeSessionState state = RuntimeSessionState.FromApi(request.State);
        string? directoryPath = string.IsNullOrWhiteSpace(request.DirectoryPath)
            ? state.CurrentDirectoryPath
            : ResolvePath(request.DirectoryPath, state.CurrentDirectoryPath);
        XmlExportResult result = _service.ExportXml(new ExportXmlRequest(directoryPath));
        XmlExportApiResponse data = result.ToApi();
        return Success(state, "XML exported.", [$"[RESULT] XML exported."], data);
    }

    private static SessionCommandResult<TData> Success<TData>(RuntimeSessionState state, string message, IReadOnlyList<string> outputLines, TData data)
    {
        return new SessionCommandResult<TData>(new OperationResult(true, message), state.ToApi(), outputLines, data);
    }

    private static SessionCommandResult<TData> Fail<TData>(RuntimeSessionState state, string message, string errorCode)
    {
        return new SessionCommandResult<TData>(new OperationResult(false, message, errorCode), state.ToApi(), [$"[ERROR] {message}"]);
    }

    private static SessionCommandResult<TData> FromOperation<TData>(RuntimeSessionState state, OperationResult operation, IReadOnlyList<string> outputLines, TData data)
    {
        return new SessionCommandResult<TData>(operation, state.ToApi(), outputLines, data);
    }

    private DirectorySnapshot BuildSnapshot()
    {
        return DirectorySnapshot.Build(_service.GetDirectoryTree());
    }

    private void RefreshStateTags(RuntimeSessionState state)
    {
        TagListResult result = _service.ListTags(new ListTagsRequest());
        RefreshStateTags(state, result);
    }

    private static void RefreshStateTags(RuntimeSessionState state, TagListResult result)
    {
        state.ReplaceTags(result.Items);
    }

    private static string ResolvePath(string rawPath, string currentDirectoryPath)
    {
        string normalizedRaw = rawPath.Replace('\\', '/').Trim();
        if (string.IsNullOrWhiteSpace(normalizedRaw) || normalizedRaw == ".")
        {
            return currentDirectoryPath;
        }

        bool isAbsolute = normalizedRaw.StartsWith("Root", StringComparison.OrdinalIgnoreCase) || normalizedRaw.StartsWith('/');
        List<string> segments = isAbsolute
            ? ["Root"]
            : currentDirectoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        foreach (string segment in normalizedRaw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count > 1)
                {
                    segments.RemoveAt(segments.Count - 1);
                }

                continue;
            }

            if (segments.Count == 0)
            {
                segments.Add("Root");
            }

            if (segments.Count == 1 &&
                segments[0].Equals("Root", StringComparison.OrdinalIgnoreCase) &&
                segment.Equals("Root", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            segments.Add(segment);
        }

        if (segments.Count == 0)
        {
            return "Root";
        }

        segments[0] = "Root";
        return string.Join('/', segments);
    }

    private static IEnumerable<DirectoryEntryResult> ApplySortEntries(IEnumerable<DirectoryEntryResult> entries, string key, string direction)
    {
        return key switch
        {
            "name" => direction == "asc"
                ? entries.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.SiblingOrder)
                : entries.OrderByDescending(item => item.Name, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.SiblingOrder),
            "size" => direction == "asc"
                ? entries.OrderBy(item => item.SizeBytes).ThenBy(item => item.SiblingOrder)
                : entries.OrderByDescending(item => item.SizeBytes).ThenBy(item => item.SiblingOrder),
            _ => direction == "asc"
                ? entries.OrderBy(item => item.Extension, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.SiblingOrder)
                : entries.OrderByDescending(item => item.Extension, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.SiblingOrder)
        };
    }

    private static bool TryFindFilePath(DirectorySnapshot snapshot, string rawPath, out string normalizedFilePath)
    {
        normalizedFilePath = string.Empty;
        string path = rawPath.Replace('\\', '/').Trim().TrimEnd('/');
        int separatorIndex = path.LastIndexOf('/');
        if (separatorIndex <= 0)
        {
            return false;
        }

        string parentPath = path[..separatorIndex];
        string fileName = path[(separatorIndex + 1)..];
        if (!snapshot.FileChildren.TryGetValue(parentPath, out List<string>? files))
        {
            return false;
        }

        string? matchedName = files.FirstOrDefault(item => item.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        if (matchedName is null)
        {
            return false;
        }

        normalizedFilePath = $"{parentPath}/{matchedName}";
        return true;
    }

    private static bool TryResolveNodePath(DirectorySnapshot snapshot, string rawPath, out string normalizedPath)
    {
        normalizedPath = rawPath.Replace('\\', '/').Trim().TrimEnd('/');
        if (snapshot.DirectoryPaths.Contains(normalizedPath))
        {
            return true;
        }

        if (TryFindFilePath(snapshot, normalizedPath, out string normalizedFilePath))
        {
            normalizedPath = normalizedFilePath;
            return true;
        }

        return false;
    }

    private static bool TryNormalizeTag(string rawTag, out string tagName, out string color)
    {
        tagName = string.Empty;
        color = string.Empty;
        string normalized = rawTag.Trim();
        foreach ((string key, string value) in SupportedTags)
        {
            if (!key.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            tagName = key;
            color = value;
            return true;
        }

        return false;
    }

    private static void DuplicateTagEntriesForCopy(RuntimeSessionState state, string sourcePrefix, string targetPrefix)
    {
        string normalizedSourcePrefix = sourcePrefix.Replace('\\', '/').Trim().TrimEnd('/');
        string normalizedTargetPrefix = targetPrefix.Replace('\\', '/').Trim().TrimEnd('/');
        List<KeyValuePair<string, HashSet<string>>> copiedEntries = state.NodeTags
            .Where(item => item.Key.Equals(normalizedSourcePrefix, StringComparison.OrdinalIgnoreCase)
                || item.Key.StartsWith($"{normalizedSourcePrefix}/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (KeyValuePair<string, HashSet<string>> sourceEntry in copiedEntries)
        {
            string suffix = sourceEntry.Key.Length == normalizedSourcePrefix.Length
                ? string.Empty
                : sourceEntry.Key[normalizedSourcePrefix.Length..];
            string copiedPath = $"{normalizedTargetPrefix}{suffix}";
            if (!state.NodeTags.TryGetValue(copiedPath, out HashSet<string>? targetTags))
            {
                targetTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                state.NodeTags[copiedPath] = targetTags;
            }

            foreach (string tag in sourceEntry.Value)
            {
                targetTags.Add(tag);
            }
        }
    }

    private static void SyncTagsWithSnapshot(RuntimeSessionState state, DirectorySnapshot snapshot)
    {
        List<string> keysToRemove = state.NodeTags.Keys
            .Where(path => !snapshot.DirectoryPaths.Contains(path) && !TryFindFilePath(snapshot, path, out _))
            .ToList();
        foreach (string key in keysToRemove)
        {
            state.NodeTags.Remove(key);
        }
    }

    private OperationResult ApplyUndoAction(RuntimeSessionState state, SessionUndoActionApiModel action, bool isRedo)
    {
        switch (action.Kind)
        {
            case SessionUndoActionKinds.SortSettingChanged:
                state.CurrentSortState = isRedo ? CloneSortState(action.CurrentSortState) : CloneSortState(action.PreviousSortState);
                return new OperationResult(true, "Sort state applied.");
            case SessionUndoActionKinds.TagAdded:
                return ApplyTagMutation(state, action, add: isRedo);
            case SessionUndoActionKinds.TagRemoved:
                return ApplyTagMutation(state, action, add: !isRedo);
            default:
                return new OperationResult(false, $"Unsupported undo action: {action.Kind}", OperationErrorCodes.ValidationFailed);
        }
    }

    private OperationResult ApplyTagMutation(RuntimeSessionState state, SessionUndoActionApiModel action, bool add)
    {
        if (string.IsNullOrWhiteSpace(action.NodePath) || string.IsNullOrWhiteSpace(action.TagName))
        {
            return new OperationResult(false, "Undo action is missing tag context.", OperationErrorCodes.ValidationFailed);
        }

        OperationResult operation = add
            ? _service.AssignTag(new AssignTagRequest(action.NodePath, action.TagName))
            : _service.RemoveTag(new RemoveTagRequest(action.NodePath, action.TagName));
        if (!operation.Success)
        {
            return operation;
        }

        RefreshStateTags(state);
        return operation;
    }

    private static bool AreSortStatesEqual(SessionSortStateApiModel? left, SessionSortStateApiModel? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return string.Equals(left.Key, right.Key, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Direction, right.Direction, StringComparison.OrdinalIgnoreCase);
    }

    private static SessionSortStateApiModel? CloneSortState(SessionSortStateApiModel? source)
    {
        return source is null ? null : new SessionSortStateApiModel(source.Key, source.Direction);
    }

    private static List<string> BuildDirectoryEntryOutputLines(string directoryPath, IEnumerable<DirectoryEntryResult> entries, SessionSortStateApiModel? sortState)
    {
        List<string> lines = [$"[RESULT] Directory: {directoryPath}"];
        if (sortState is not null)
        {
            lines.Add($"[INFO] Applied sort: {sortState.Key} {sortState.Direction}");
        }

        DirectoryEntryResult[] items = entries.ToArray();
        if (items.Length == 0)
        {
            lines.Add("[RESULT] (empty)");
            return lines;
        }

        foreach (DirectoryEntryResult entry in items)
        {
            if (entry.IsDirectory)
            {
                lines.Add($"[Dir]  {entry.FullPath} (size: {entry.FormattedSize})");
                continue;
            }

            string extension = string.IsNullOrWhiteSpace(entry.Extension) ? "(none)" : entry.Extension;
            lines.Add($"[File] {entry.FullPath} (ext: {extension}, size: {entry.FormattedSize})");
        }

        return lines;
    }

    private static List<string> BuildTagListOutputLines(List<TaggedNodeApiResponse> items)
    {
        if (items.Count == 0)
        {
            return ["[INFO] No tags assigned."];
        }

        List<string> lines = [];
        foreach (TaggedNodeApiResponse item in items)
        {
            string tagsText = string.Join(", ", item.Tags.Select(tag => $"{tag}({SupportedTags[tag]})"));
            lines.Add($"{item.Path}: {tagsText}");
        }

        return lines;
    }

    private static List<string> BuildTagFindOutputLines(TagFindResultApiResponse data)
    {
        List<string> lines =
        [
            $"[RESULT] Tag: {data.Tag} ({data.Color})",
            $"[RESULT] Scope: {data.ScopePath}"
        ];
        if (data.Paths.Count == 0)
        {
            lines.Add("[RESULT] No match.");
            return lines;
        }

        lines.AddRange(data.Paths);
        return lines;
    }

    private static List<string> BuildSearchOutputLines(SearchApiResponse data)
    {
        if (data.Paths.Count == 0)
        {
            return ["[RESULT] No match."];
        }

        return data.Paths.ToList();
    }

    private sealed class RuntimeSessionState
    {
        public RuntimeSessionState()
        {
            CurrentDirectoryPath = "Root";
            NodeTags = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            UndoStack = new Stack<SessionUndoActionApiModel>();
            RedoStack = new Stack<SessionUndoActionApiModel>();
        }

        public string CurrentDirectoryPath { get; set; }

        public SessionClipboardItemApiModel? ClipboardItem { get; set; }

        public SessionSortStateApiModel? CurrentSortState { get; set; }

        public Dictionary<string, HashSet<string>> NodeTags { get; }

        public Stack<SessionUndoActionApiModel> UndoStack { get; }

        public Stack<SessionUndoActionApiModel> RedoStack { get; }

        public static RuntimeSessionState FromApi(ClientSessionStateApiModel? state)
        {
            RuntimeSessionState result = new();
            if (state is null)
            {
                return result;
            }

            result.CurrentDirectoryPath = string.IsNullOrWhiteSpace(state.CurrentDirectoryPath)
                ? "Root"
                : state.CurrentDirectoryPath;
            result.ClipboardItem = state.ClipboardItem is null
                ? null
                : new SessionClipboardItemApiModel(state.ClipboardItem.IsDirectory, state.ClipboardItem.Path);
            result.CurrentSortState = CloneSortState(state.CurrentSortState);

            foreach ((string key, IReadOnlyList<string> value) in state.NodeTags)
            {
                result.NodeTags[key] = new HashSet<string>(value, StringComparer.OrdinalIgnoreCase);
            }

            foreach (SessionUndoActionApiModel action in state.UndoStack.Reverse())
            {
                result.UndoStack.Push(CloneUndoAction(action));
            }

            foreach (SessionUndoActionApiModel action in state.RedoStack.Reverse())
            {
                result.RedoStack.Push(CloneUndoAction(action));
            }

            return result;
        }

        public ClientSessionStateApiModel ToApi()
        {
            return new ClientSessionStateApiModel
            {
                CurrentDirectoryPath = CurrentDirectoryPath,
                ClipboardItem = ClipboardItem is null ? null : new SessionClipboardItemApiModel(ClipboardItem.IsDirectory, ClipboardItem.Path),
                CurrentSortState = CloneSortState(CurrentSortState),
                NodeTags = NodeTags.ToDictionary(
                    item => item.Key,
                    item => (IReadOnlyList<string>)item.Value.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToArray(),
                    StringComparer.OrdinalIgnoreCase),
                UndoStack = UndoStack.Select(CloneUndoAction).ToArray(),
                RedoStack = RedoStack.Select(CloneUndoAction).ToArray()
            };
        }

        public void RecordUndoAction(SessionUndoActionApiModel action)
        {
            UndoStack.Push(CloneUndoAction(action));
            RedoStack.Clear();
        }

        public void ReplaceTags(IReadOnlyList<TaggedNodeResult> items)
        {
            NodeTags.Clear();
            foreach (TaggedNodeResult item in items)
            {
                NodeTags[item.Path] = new HashSet<string>(item.Tags.Select(tag => tag.Name), StringComparer.OrdinalIgnoreCase);
            }
        }

        private static SessionUndoActionApiModel CloneUndoAction(SessionUndoActionApiModel source)
        {
            return new SessionUndoActionApiModel
            {
                Kind = source.Kind,
                PreviousSortState = CloneSortState(source.PreviousSortState),
                CurrentSortState = CloneSortState(source.CurrentSortState),
                NodePath = source.NodePath,
                TagName = source.TagName,
                TagColor = source.TagColor
            };
        }
    }

    private sealed class DirectorySnapshot
    {
        public DirectorySnapshot(HashSet<string> directoryPaths, Dictionary<string, List<string>> directoryChildren, Dictionary<string, List<string>> fileChildren)
        {
            DirectoryPaths = directoryPaths;
            DirectoryChildren = directoryChildren;
            FileChildren = fileChildren;
        }

        public HashSet<string> DirectoryPaths { get; }

        public Dictionary<string, List<string>> DirectoryChildren { get; }

        public Dictionary<string, List<string>> FileChildren { get; }

        public static DirectorySnapshot Build(DirectoryTreeResult tree)
        {
            HashSet<string> directoryPaths = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<string>> directoryChildren = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<string>> fileChildren = new(StringComparer.OrdinalIgnoreCase);
            AddDirectoryRecursively(tree.Root, tree.Root.Name, directoryPaths, directoryChildren, fileChildren);
            return new DirectorySnapshot(directoryPaths, directoryChildren, fileChildren);
        }

        private static void AddDirectoryRecursively(
            DirectoryNodeResult directory,
            string path,
            ISet<string> directoryPaths,
            IDictionary<string, List<string>> directoryChildren,
            IDictionary<string, List<string>> fileChildren)
        {
            directoryPaths.Add(path);
            if (!directoryChildren.TryGetValue(path, out List<string>? directoryItems))
            {
                directoryItems = [];
                directoryChildren[path] = directoryItems;
            }

            if (!fileChildren.TryGetValue(path, out List<string>? fileItems))
            {
                fileItems = [];
                fileChildren[path] = fileItems;
            }

            foreach (DirectoryNodeResult childDirectory in directory.Directories)
            {
                directoryItems.Add(childDirectory.Name);
                string childPath = $"{path}/{childDirectory.Name}";
                AddDirectoryRecursively(childDirectory, childPath, directoryPaths, directoryChildren, fileChildren);
            }

            foreach (FileNodeResult file in directory.Files)
            {
                fileItems.Add(file.Name);
            }
        }
    }
}

public static class SessionUndoActionKinds
{
    public const string SortSettingChanged = "SortSettingChanged";
    public const string TagAdded = "TagAdded";
    public const string TagRemoved = "TagRemoved";
}

internal static class DirectoryEntryApiResponseMapper
{
    public static DirectoryEntryApiResponse ToApi(this DirectoryEntryResult entry)
    {
        return new DirectoryEntryApiResponse(
            entry.Name,
            entry.IsDirectory,
            entry.FullPath,
            entry.SizeBytes,
            entry.FormattedSize,
            entry.Extension,
            entry.SiblingOrder);
    }
}
