using CloudFileManager.Application.Models;

namespace CloudFileManager.Presentation.ConsoleApp;

public sealed partial class ConsoleCommandExecutor
{
    private static readonly Dictionary<string, string> SupportedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Urgent"] = "Red",
        ["Work"] = "Blue",
        ["Personal"] = "Green"
    };

    private void HandleChangeDirectory(IReadOnlyList<string> args)
    {
        if (args.Count < 1)
        {
            PrintUsage("cd <directoryPath>");
            return;
        }

        string targetPath = ResolveDirectoryPath(args[0]);
        ConsoleDirectorySnapshot snapshot = ConsoleDirectorySnapshotBuilder.Build(_service.GetDirectoryTree());
        if (!snapshot.DirectoryPaths.Contains(targetPath))
        {
            PrintError($"Directory not found: {targetPath}");
            return;
        }

        _sessionState.CurrentDirectoryPath = targetPath;
    }

    private void HandleList(IReadOnlyList<string> args)
    {
        string targetPath = args.Count == 0 ? CurrentDirectoryPath : ResolveDirectoryPath(args[0]);
        ConsoleDirectorySnapshot snapshot = ConsoleDirectorySnapshotBuilder.Build(_service.GetDirectoryTree());
        if (!snapshot.DirectoryPaths.Contains(targetPath))
        {
            PrintError($"Directory not found: {targetPath}");
            return;
        }

        if (!snapshot.DirectoryChildren.TryGetValue(targetPath, out List<string>? directories))
        {
            directories = [];
        }

        if (!snapshot.FileChildren.TryGetValue(targetPath, out List<string>? files))
        {
            files = [];
        }

        if (directories.Count == 0 && files.Count == 0)
        {
            System.Console.WriteLine("[RESULT] (empty)");
            return;
        }

        foreach (string directory in directories.OrderBy(item => item, StringComparer.OrdinalIgnoreCase))
        {
            System.Console.WriteLine($"[Dir]  {directory}");
        }

        foreach (string file in files.OrderBy(item => item, StringComparer.OrdinalIgnoreCase))
        {
            System.Console.WriteLine($"[File] {file}");
        }
    }

    private void HandleCreateDirectory(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            PrintUsage("mkdir <directoryName> OR mkdir <parentPath> <directoryName>");
            return;
        }

        string parentPath = args.Count == 1 ? CurrentDirectoryPath : ResolveDirectoryPath(args[0]);
        string directoryName = args.Count == 1 ? args[0] : args[1];

        OperationResult result = _service.CreateDirectory(new CreateDirectoryRequest(parentPath, directoryName));
        PrintResult(result.Success, result.Message);
    }

    private void HandleCopy(IReadOnlyList<string> args)
    {
        if (args.Count != 1)
        {
            PrintUsage("copy <sourcePath>");
            return;
        }

        string sourcePath = ResolveDirectoryPath(args[0]);
        ConsoleDirectorySnapshot snapshot = ConsoleDirectorySnapshotBuilder.Build(_service.GetDirectoryTree());
        if (snapshot.DirectoryPaths.Contains(sourcePath))
        {
            _sessionState.ClipboardItem = new ConsoleClipboardItem(IsDirectory: true, sourcePath);
            PrintResult(true, $"Copied directory: {sourcePath}");
            return;
        }

        if (TryFindFilePath(snapshot, sourcePath, out string normalizedFilePath))
        {
            _sessionState.ClipboardItem = new ConsoleClipboardItem(IsDirectory: false, normalizedFilePath);
            PrintResult(true, $"Copied file: {normalizedFilePath}");
            return;
        }

        PrintError($"Source not found: {sourcePath}");
    }

    private void HandlePaste(IReadOnlyList<string> args)
    {
        if (args.Count > 1)
        {
            PrintUsage("paste [targetDirectoryPath]");
            return;
        }

        if (_sessionState.ClipboardItem is null)
        {
            PrintError("Clipboard is empty. Use copy first.");
            return;
        }

        string targetDirectoryPath = args.Count == 0 ? CurrentDirectoryPath : ResolveDirectoryPath(args[0]);
        OperationResult result = _sessionState.ClipboardItem.IsDirectory
            ? _service.CopyDirectory(new CopyDirectoryRequest(_sessionState.ClipboardItem.Path, targetDirectoryPath))
            : _service.CopyFile(new CopyFileRequest(_sessionState.ClipboardItem.Path, targetDirectoryPath));

        if (result.Success)
        {
            string sourcePath = _sessionState.ClipboardItem.Path.Replace('\\', '/').Trim().TrimEnd('/');
            string sourceName = sourcePath[(sourcePath.LastIndexOf('/') + 1)..];
            string targetPath = $"{targetDirectoryPath.TrimEnd('/')}/{sourceName}";
            DuplicateTagEntriesForCopy(sourcePath, targetPath);
        }

        PrintResult(result.Success, result.Message);
    }

    private void HandleTag(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            PrintUsage("tag <path> <Urgent|Work|Personal> | tag remove <path> <tag> | tag list [path] | tag find <tag> [directoryPath]");
            return;
        }

        string action = args[0].ToLowerInvariant();
        if (action == "list")
        {
            HandleTagList(args);
            return;
        }

        if (action == "find")
        {
            HandleTagFind(args);
            return;
        }

        if (action == "remove")
        {
            HandleTagRemove(args);
            return;
        }

        if (args.Count != 2)
        {
            PrintUsage("tag <path> <Urgent|Work|Personal>");
            return;
        }

        string path = ResolveDirectoryPath(args[0]);
        ConsoleDirectorySnapshot snapshot = ConsoleDirectorySnapshotBuilder.Build(_service.GetDirectoryTree());
        SyncTagsWithSnapshot(snapshot);
        if (!TryResolveNodePath(snapshot, path, out string normalizedPath, out bool _))
        {
            PrintError($"Node not found: {path}");
            return;
        }

        if (!TryNormalizeTag(args[1], out string tagName, out string color))
        {
            PrintError($"Unsupported tag: {args[1]}");
            return;
        }

        if (!_sessionState.NodeTags.TryGetValue(normalizedPath, out HashSet<string>? tags))
        {
            tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _sessionState.NodeTags[normalizedPath] = tags;
        }

        bool added = tags.Add(tagName);
        if (added)
        {
            _sessionState.MarkDirty();
            RecordUndoAction(new ConsoleUndoAction(
                ConsoleUndoActionKind.TagAdded,
                NodePath: normalizedPath,
                TagName: tagName,
                TagColor: color));
        }

        PrintResult(added, added
            ? $"Tag assigned: {tagName} ({color}) -> {normalizedPath}"
            : $"Tag already exists: {tagName} -> {normalizedPath}");
    }

    private void HandleUpload(IReadOnlyList<string> args)
    {
        if (args.Count == 0 || args.Count > 2)
        {
            PrintUsage("upload <localFilePath> OR upload <directoryPath> <localFilePath>");
            return;
        }

        string targetDirectoryPath = args.Count == 1 ? CurrentDirectoryPath : ResolveDirectoryPath(args[0]);
        string localPathInput = args.Count == 1 ? args[0] : args[1];
        string localPath = Path.GetFullPath(localPathInput);

        if (!File.Exists(localPath))
        {
            PrintError($"Local file not found: {localPath}");
            return;
        }

        try
        {
            UploadFileRequest request = _localFileUploadRequestFactory.Create(targetDirectoryPath, Path.GetFileName(localPath), localPath);
            OperationResult result = _service.UploadFile(request);
            PrintResult(result.Success, result.Message);
        }
        catch (IOException ex)
        {
            PrintResult(false, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            PrintResult(false, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            PrintResult(false, ex.Message);
        }
        catch
        {
            PrintResult(false, "Upload failed due to an unexpected error.");
        }
    }

    private void HandleMoveFile(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
        {
            PrintUsage("move-file <sourceFilePath> <targetDirectoryPath>");
            return;
        }

        string sourceFilePath = ResolveFilePath(args[0]);
        string targetDirectoryPath = ResolveDirectoryPath(args[1]);
        string movedFileName = sourceFilePath[(sourceFilePath.LastIndexOf('/') + 1)..];
        string movedFileTargetPath = $"{targetDirectoryPath.TrimEnd('/')}/{movedFileName}";

        OperationResult result = _service.MoveFile(new MoveFileRequest(sourceFilePath, targetDirectoryPath));
        if (result.Success)
        {
            RebaseTagEntries(sourceFilePath, movedFileTargetPath);
        }

        PrintResult(result.Success, result.Message);
    }

    private void HandleRenameFile(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
        {
            PrintUsage("rename-file <filePath> <newFileName>");
            return;
        }

        string sourceFilePath = ResolveFilePath(args[0]);
        int separatorIndex = sourceFilePath.LastIndexOf('/');
        string parentPath = separatorIndex > 0 ? sourceFilePath[..separatorIndex] : "Root";
        string targetFilePath = $"{parentPath}/{args[1]}";

        OperationResult result = _service.RenameFile(new RenameFileRequest(sourceFilePath, args[1]));
        if (result.Success)
        {
            RebaseTagEntries(sourceFilePath, targetFilePath);
        }

        PrintResult(result.Success, result.Message);
    }

    private void HandleDeleteFile(IReadOnlyList<string> args)
    {
        if (args.Count < 1)
        {
            PrintUsage("delete-file <filePath>");
            return;
        }

        string filePath = ResolveFilePath(args[0]);
        OperationResult result = _service.DeleteFile(new DeleteFileRequest(filePath));
        if (result.Success)
        {
            RemoveTagEntriesByPrefix(filePath);
        }

        PrintResult(result.Success, result.Message);
    }

    private void HandleDownload(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
        {
            PrintUsage("download <filePath> <targetLocalPath>");
            return;
        }

        OperationResult result = _service.DownloadFile(new DownloadFileRequest(ResolveFilePath(args[0]), args[1]));
        PrintResult(result.Success, result.Message);
    }

    private void HandleMoveDirectory(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
        {
            PrintUsage("move-dir <sourceDirectoryPath> <targetParentDirectoryPath>");
            return;
        }

        string sourceDirectoryPath = ResolveDirectoryPath(args[0]);
        string targetParentDirectoryPath = ResolveDirectoryPath(args[1]);
        string movedDirectoryName = sourceDirectoryPath[(sourceDirectoryPath.LastIndexOf('/') + 1)..];
        string movedDirectoryTargetPath = $"{targetParentDirectoryPath.TrimEnd('/')}/{movedDirectoryName}";

        OperationResult result = _service.MoveDirectory(new MoveDirectoryRequest(sourceDirectoryPath, targetParentDirectoryPath));
        if (result.Success)
        {
            RebaseTagEntries(sourceDirectoryPath, movedDirectoryTargetPath);
        }

        PrintResult(result.Success, result.Message);
    }

    private void HandleRenameDirectory(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
        {
            PrintUsage("rename-dir <directoryPath> <newDirectoryName>");
            return;
        }

        string sourceDirectoryPath = ResolveDirectoryPath(args[0]);
        int separatorIndex = sourceDirectoryPath.LastIndexOf('/');
        string parentPath = separatorIndex > 0 ? sourceDirectoryPath[..separatorIndex] : "Root";
        string targetDirectoryPath = $"{parentPath}/{args[1]}";

        OperationResult result = _service.RenameDirectory(new RenameDirectoryRequest(sourceDirectoryPath, args[1]));
        if (result.Success)
        {
            RebaseTagEntries(sourceDirectoryPath, targetDirectoryPath);
        }

        PrintResult(result.Success, result.Message);
    }

    private void HandleDeleteDirectory(IReadOnlyList<string> args)
    {
        if (args.Count < 1)
        {
            PrintUsage("delete-dir <directoryPath>");
            return;
        }

        string directoryPath = ResolveDirectoryPath(args[0]);
        OperationResult result = _service.DeleteDirectory(new DeleteDirectoryRequest(directoryPath));
        if (result.Success)
        {
            RemoveTagEntriesByPrefix(directoryPath);
        }

        PrintResult(result.Success, result.Message);
    }

    private void HandleTagList(IReadOnlyList<string> args)
    {
        if (args.Count > 2)
        {
            PrintUsage("tag list [path]");
            return;
        }

        ConsoleDirectorySnapshot snapshot = ConsoleDirectorySnapshotBuilder.Build(_service.GetDirectoryTree());
        SyncTagsWithSnapshot(snapshot);

        if (args.Count == 1)
        {
            if (_sessionState.NodeTags.Count == 0)
            {
                PrintInfo("No tags assigned.");
                return;
            }

            PrintSectionHeader("TAG LIST");
            foreach (KeyValuePair<string, HashSet<string>> item in _sessionState.NodeTags.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                string tagsText = string.Join(", ", item.Value.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).Select(tag => $"{tag}({SupportedTags[tag]})"));
                System.Console.WriteLine($"{item.Key}: {tagsText}");
            }

            PrintSectionFooter("TAG LIST");
            return;
        }

        string path = ResolveDirectoryPath(args[1]);
        if (!TryResolveNodePath(snapshot, path, out string normalizedPath, out bool _))
        {
            PrintError($"Node not found: {path}");
            return;
        }

        if (!_sessionState.NodeTags.TryGetValue(normalizedPath, out HashSet<string>? tags) || tags.Count == 0)
        {
            PrintInfo($"No tags on node: {normalizedPath}");
            return;
        }

        PrintSectionHeader("TAG LIST");
        foreach (string tag in tags.OrderBy(item => item, StringComparer.OrdinalIgnoreCase))
        {
            System.Console.WriteLine($"{normalizedPath}: {tag} ({SupportedTags[tag]})");
        }

        PrintSectionFooter("TAG LIST");
    }

    private void HandleTagFind(IReadOnlyList<string> args)
    {
        if (args.Count < 2 || args.Count > 3)
        {
            PrintUsage("tag find <Urgent|Work|Personal> [directoryPath]");
            return;
        }

        if (!TryNormalizeTag(args[1], out string tagName, out string color))
        {
            PrintError($"Unsupported tag: {args[1]}");
            return;
        }

        string scopePath = args.Count == 3 ? ResolveDirectoryPath(args[2]) : CurrentDirectoryPath;
        ConsoleDirectorySnapshot snapshot = ConsoleDirectorySnapshotBuilder.Build(_service.GetDirectoryTree());
        SyncTagsWithSnapshot(snapshot);
        if (!snapshot.DirectoryPaths.Contains(scopePath))
        {
            PrintError($"Directory not found: {scopePath}");
            return;
        }

        List<string> matchedPaths = _sessionState.NodeTags
            .Where(item => item.Value.Contains(tagName) && IsPathInScope(item.Key, scopePath))
            .Select(item => item.Key)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();

        PrintSectionHeader("TAG FIND RESULT");
        System.Console.WriteLine($"[RESULT] Tag: {tagName} ({color})");
        System.Console.WriteLine($"[RESULT] Scope: {scopePath}");
        if (matchedPaths.Count == 0)
        {
            System.Console.WriteLine("[RESULT] No match.");
            PrintSectionFooter("TAG FIND RESULT");
            return;
        }

        foreach (string path in matchedPaths)
        {
            System.Console.WriteLine(path);
        }

        PrintSectionFooter("TAG FIND RESULT");
    }

    private void HandleTagRemove(IReadOnlyList<string> args)
    {
        if (args.Count != 3)
        {
            PrintUsage("tag remove <path> <Urgent|Work|Personal>");
            return;
        }

        string path = ResolveDirectoryPath(args[1]);
        ConsoleDirectorySnapshot snapshot = ConsoleDirectorySnapshotBuilder.Build(_service.GetDirectoryTree());
        SyncTagsWithSnapshot(snapshot);
        if (!TryResolveNodePath(snapshot, path, out string normalizedPath, out bool _))
        {
            PrintError($"Node not found: {path}");
            return;
        }

        if (!TryNormalizeTag(args[2], out string tagName, out string _))
        {
            PrintError($"Unsupported tag: {args[2]}");
            return;
        }

        if (!_sessionState.NodeTags.TryGetValue(normalizedPath, out HashSet<string>? tags) || !tags.Remove(tagName))
        {
            PrintResult(false, $"Tag not found on node: {tagName} -> {normalizedPath}");
            return;
        }

        if (tags.Count == 0)
        {
            _sessionState.NodeTags.Remove(normalizedPath);
        }

        _sessionState.MarkDirty();
        RecordUndoAction(new ConsoleUndoAction(
            ConsoleUndoActionKind.TagRemoved,
            NodePath: normalizedPath,
            TagName: tagName,
            TagColor: SupportedTags[tagName]));

        PrintResult(true, $"Tag removed: {tagName} -> {normalizedPath}");
    }

    private void HandleUndo()
    {
        if (_sessionState.UndoStack.Count == 0)
        {
            PrintError("Nothing to undo.");
            return;
        }

        ConsoleUndoAction action = _sessionState.UndoStack.Pop();
        ApplyUndoAction(action, isRedo: false);
        _sessionState.RedoStack.Push(action);
        _sessionState.MarkDirty();
        PrintResult(true, $"Undo: {action.Kind}");
    }

    private void HandleRedo()
    {
        if (_sessionState.RedoStack.Count == 0)
        {
            PrintError("Nothing to redo.");
            return;
        }

        ConsoleUndoAction action = _sessionState.RedoStack.Pop();
        ApplyUndoAction(action, isRedo: true);
        _sessionState.UndoStack.Push(action);
        _sessionState.MarkDirty();
        PrintResult(true, $"Redo: {action.Kind}");
    }

    private static bool TryFindFilePath(ConsoleDirectorySnapshot snapshot, string rawPath, out string normalizedFilePath)
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

    private static bool TryResolveNodePath(ConsoleDirectorySnapshot snapshot, string rawPath, out string normalizedPath, out bool isDirectory)
    {
        normalizedPath = rawPath.Replace('\\', '/').Trim().TrimEnd('/');
        if (snapshot.DirectoryPaths.Contains(normalizedPath))
        {
            isDirectory = true;
            return true;
        }

        if (TryFindFilePath(snapshot, normalizedPath, out string normalizedFilePath))
        {
            normalizedPath = normalizedFilePath;
            isDirectory = false;
            return true;
        }

        isDirectory = false;
        return false;
    }

    private static bool TryNormalizeTag(string rawTag, out string tagName, out string color)
    {
        tagName = string.Empty;
        color = string.Empty;
        string normalized = rawTag.Trim();
        foreach (KeyValuePair<string, string> tag in SupportedTags)
        {
            if (!tag.Key.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            tagName = tag.Key;
            color = tag.Value;
            return true;
        }

        return false;
    }

    private void RemoveTagEntriesByPrefix(string pathPrefix)
    {
        string normalizedPrefix = pathPrefix.Replace('\\', '/').Trim().TrimEnd('/');
        List<string> keysToRemove = _sessionState.NodeTags.Keys
            .Where(path => path.Equals(normalizedPrefix, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith($"{normalizedPrefix}/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (string key in keysToRemove)
        {
            _sessionState.NodeTags.Remove(key);
        }

        if (keysToRemove.Count > 0)
        {
            _sessionState.MarkDirty();
        }
    }

    private void RebaseTagEntries(string oldPrefix, string newPrefix)
    {
        string normalizedOldPrefix = oldPrefix.Replace('\\', '/').Trim().TrimEnd('/');
        string normalizedNewPrefix = newPrefix.Replace('\\', '/').Trim().TrimEnd('/');
        List<KeyValuePair<string, HashSet<string>>> movedEntries = _sessionState.NodeTags
            .Where(item => item.Key.Equals(normalizedOldPrefix, StringComparison.OrdinalIgnoreCase)
                || item.Key.StartsWith($"{normalizedOldPrefix}/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (KeyValuePair<string, HashSet<string>> entry in movedEntries)
        {
            _sessionState.NodeTags.Remove(entry.Key);
            string suffix = entry.Key.Length == normalizedOldPrefix.Length ? string.Empty : entry.Key[normalizedOldPrefix.Length..];
            string targetPath = $"{normalizedNewPrefix}{suffix}";
            _sessionState.NodeTags[targetPath] = new HashSet<string>(entry.Value, StringComparer.OrdinalIgnoreCase);
        }

        if (movedEntries.Count > 0)
        {
            _sessionState.MarkDirty();
        }
    }

    private void DuplicateTagEntriesForCopy(string sourcePrefix, string targetPrefix)
    {
        string normalizedSourcePrefix = sourcePrefix.Replace('\\', '/').Trim().TrimEnd('/');
        string normalizedTargetPrefix = targetPrefix.Replace('\\', '/').Trim().TrimEnd('/');

        List<KeyValuePair<string, HashSet<string>>> copiedEntries = _sessionState.NodeTags
            .Where(item => item.Key.Equals(normalizedSourcePrefix, StringComparison.OrdinalIgnoreCase)
                || item.Key.StartsWith($"{normalizedSourcePrefix}/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (KeyValuePair<string, HashSet<string>> sourceEntry in copiedEntries)
        {
            string suffix = sourceEntry.Key.Length == normalizedSourcePrefix.Length
                ? string.Empty
                : sourceEntry.Key[normalizedSourcePrefix.Length..];
            string copiedPath = $"{normalizedTargetPrefix}{suffix}";

            if (!_sessionState.NodeTags.TryGetValue(copiedPath, out HashSet<string>? targetTags))
            {
                targetTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _sessionState.NodeTags[copiedPath] = targetTags;
            }

            foreach (string tag in sourceEntry.Value)
            {
                targetTags.Add(tag);
            }
        }

        if (copiedEntries.Count > 0)
        {
            _sessionState.MarkDirty();
        }
    }

    private void RecordUndoAction(ConsoleUndoAction action)
    {
        _sessionState.UndoStack.Push(action);
        _sessionState.RedoStack.Clear();
    }

    private void ApplyUndoAction(ConsoleUndoAction action, bool isRedo)
    {
        switch (action.Kind)
        {
            case ConsoleUndoActionKind.SortSettingChanged:
                _sessionState.CurrentSortState = isRedo ? action.CurrentSortState : action.PreviousSortState;
                if (_sessionState.CurrentSortState is null)
                {
                    PrintInfo("Sort setting cleared.");
                }
                else
                {
                    PrintInfo($"Sort setting: {_sessionState.CurrentSortState.Key} {_sessionState.CurrentSortState.Direction}");
                }

                break;
            case ConsoleUndoActionKind.TagAdded:
                ApplyTagMutation(action, add: isRedo);
                break;
            case ConsoleUndoActionKind.TagRemoved:
                ApplyTagMutation(action, add: !isRedo);
                break;
        }
    }

    private void ApplyTagMutation(ConsoleUndoAction action, bool add)
    {
        if (string.IsNullOrWhiteSpace(action.NodePath) || string.IsNullOrWhiteSpace(action.TagName))
        {
            return;
        }

        if (add)
        {
            if (!_sessionState.NodeTags.TryGetValue(action.NodePath, out HashSet<string>? tags))
            {
                tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _sessionState.NodeTags[action.NodePath] = tags;
            }

            tags.Add(action.TagName);
            return;
        }

        if (!_sessionState.NodeTags.TryGetValue(action.NodePath, out HashSet<string>? removeTags))
        {
            return;
        }

        removeTags.Remove(action.TagName);
        if (removeTags.Count == 0)
        {
            _sessionState.NodeTags.Remove(action.NodePath);
        }
    }

    private void SyncTagsWithSnapshot(ConsoleDirectorySnapshot snapshot)
    {
        List<string> keysToRemove = _sessionState.NodeTags.Keys
            .Where(path => !snapshot.DirectoryPaths.Contains(path) && !TryFindFilePath(snapshot, path, out _))
            .ToList();

        foreach (string key in keysToRemove)
        {
            _sessionState.NodeTags.Remove(key);
        }

        if (keysToRemove.Count > 0)
        {
            _sessionState.MarkDirty();
        }
    }

    private static bool IsPathInScope(string nodePath, string scopePath)
    {
        string normalizedScope = scopePath.Replace('\\', '/').Trim().TrimEnd('/');
        return nodePath.Equals(normalizedScope, StringComparison.OrdinalIgnoreCase)
            || nodePath.StartsWith($"{normalizedScope}/", StringComparison.OrdinalIgnoreCase);
    }
}
