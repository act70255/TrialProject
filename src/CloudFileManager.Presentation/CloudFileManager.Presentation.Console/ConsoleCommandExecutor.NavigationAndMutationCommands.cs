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
            RefreshSessionTagsFromPersistence();
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

        OperationResult result = _service.AssignTag(new AssignTagRequest(normalizedPath, tagName));
        bool added = result.Success && result.Message.StartsWith("Tag assigned:", StringComparison.OrdinalIgnoreCase);
        if (added)
        {
            RecordUndoAction(new ConsoleUndoAction(
                ConsoleUndoActionKind.TagAdded,
                NodePath: normalizedPath,
                TagName: tagName,
                TagColor: color));
        }

        if (result.Success)
        {
            RefreshSessionTagsFromPersistence();
        }

        PrintResult(result.Success, result.Message);
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

        OperationResult result = _service.MoveFile(new MoveFileRequest(sourceFilePath, targetDirectoryPath));
        if (result.Success)
        {
            RefreshSessionTagsFromPersistence();
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

        OperationResult result = _service.RenameFile(new RenameFileRequest(sourceFilePath, args[1]));
        if (result.Success)
        {
            RefreshSessionTagsFromPersistence();
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
            RefreshSessionTagsFromPersistence();
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

        OperationResult result = _service.MoveDirectory(new MoveDirectoryRequest(sourceDirectoryPath, targetParentDirectoryPath));
        if (result.Success)
        {
            RefreshSessionTagsFromPersistence();
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

        OperationResult result = _service.RenameDirectory(new RenameDirectoryRequest(sourceDirectoryPath, args[1]));
        if (result.Success)
        {
            RefreshSessionTagsFromPersistence();
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
            RefreshSessionTagsFromPersistence();
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

        if (args.Count == 1)
        {
            TagListResult allTagResult = _service.ListTags(new ListTagsRequest());
            if (allTagResult.Items.Count == 0)
            {
                PrintInfo("No tags assigned.");
                return;
            }

            PrintSectionHeader("TAG LIST");
            foreach (TaggedNodeResult item in allTagResult.Items.OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase))
            {
                string tagsText = string.Join(", ", item.Tags.OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase).Select(tag => $"{tag.Name}({tag.Color})"));
                System.Console.WriteLine($"{item.Path}: {tagsText}");
            }

            PrintSectionFooter("TAG LIST");
            return;
        }

        string path = ResolveDirectoryPath(args[1]);
        ConsoleDirectorySnapshot snapshot = ConsoleDirectorySnapshotBuilder.Build(_service.GetDirectoryTree());
        if (!TryResolveNodePath(snapshot, path, out string normalizedPath, out bool _))
        {
            PrintError($"Node not found: {path}");
            return;
        }

        TagListResult scopedTagResult = _service.ListTags(new ListTagsRequest(normalizedPath));
        if (scopedTagResult.Items.Count == 0)
        {
            PrintInfo($"No tags on node: {normalizedPath}");
            return;
        }

        PrintSectionHeader("TAG LIST");
        foreach (TaggedNodeResult item in scopedTagResult.Items)
        {
            foreach (TagInfoResult tag in item.Tags.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                System.Console.WriteLine($"{item.Path}: {tag.Name} ({tag.Color})");
            }
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

        if (!TryNormalizeTag(args[1], out string tagName, out string _))
        {
            PrintError($"Unsupported tag: {args[1]}");
            return;
        }

        string scopePath = args.Count == 3 ? ResolveDirectoryPath(args[2]) : CurrentDirectoryPath;
        ConsoleDirectorySnapshot snapshot = ConsoleDirectorySnapshotBuilder.Build(_service.GetDirectoryTree());
        if (!snapshot.DirectoryPaths.Contains(scopePath))
        {
            PrintError($"Directory not found: {scopePath}");
            return;
        }

        TagFindResult findResult = _service.FindTags(new FindTagsRequest(tagName, scopePath));

        PrintSectionHeader("TAG FIND RESULT");
        System.Console.WriteLine($"[RESULT] Tag: {findResult.Tag} ({findResult.Color})");
        System.Console.WriteLine($"[RESULT] Scope: {findResult.ScopePath}");
        if (findResult.Paths.Count == 0)
        {
            System.Console.WriteLine("[RESULT] No match.");
            PrintSectionFooter("TAG FIND RESULT");
            return;
        }

        foreach (string path in findResult.Paths)
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

        OperationResult result = _service.RemoveTag(new RemoveTagRequest(normalizedPath, tagName));
        if (result.Success)
        {
            RecordUndoAction(new ConsoleUndoAction(
                ConsoleUndoActionKind.TagRemoved,
                NodePath: normalizedPath,
                TagName: tagName,
                TagColor: SupportedTags[tagName]));
            RefreshSessionTagsFromPersistence();
        }

        PrintResult(result.Success, result.Message);
    }

    private void HandleUndo()
    {
        if (_sessionState.UndoStack.Count == 0)
        {
            PrintError("Nothing to undo.");
            return;
        }

        ConsoleUndoAction action = _sessionState.UndoStack.Pop();
        if (!ApplyUndoAction(action, isRedo: false, out string? message))
        {
            _sessionState.UndoStack.Push(action);
            PrintResult(false, message ?? "Undo failed.");
            return;
        }

        _sessionState.RedoStack.Push(action);
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
        if (!ApplyUndoAction(action, isRedo: true, out string? message))
        {
            _sessionState.RedoStack.Push(action);
            PrintResult(false, message ?? "Redo failed.");
            return;
        }

        _sessionState.UndoStack.Push(action);
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

    private void RecordUndoAction(ConsoleUndoAction action)
    {
        _sessionState.UndoStack.Push(action);
        _sessionState.RedoStack.Clear();
    }

    private bool ApplyUndoAction(ConsoleUndoAction action, bool isRedo, out string? message)
    {
        message = null;
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

                return true;
            case ConsoleUndoActionKind.TagAdded:
                return ApplyTagMutation(action, add: isRedo, out message);
            case ConsoleUndoActionKind.TagRemoved:
                return ApplyTagMutation(action, add: !isRedo, out message);
            default:
                message = $"Unsupported undo action: {action.Kind}";
                return false;
        }
    }

    private bool ApplyTagMutation(ConsoleUndoAction action, bool add, out string? message)
    {
        message = null;
        if (string.IsNullOrWhiteSpace(action.NodePath) || string.IsNullOrWhiteSpace(action.TagName))
        {
            message = "Undo action is missing tag context.";
            return false;
        }

        OperationResult result = add
            ? _service.AssignTag(new AssignTagRequest(action.NodePath, action.TagName))
            : _service.RemoveTag(new RemoveTagRequest(action.NodePath, action.TagName));
        if (!result.Success)
        {
            message = result.Message;
            return false;
        }

        RefreshSessionTagsFromPersistence();
        return true;
    }

    private void RefreshSessionTagsFromPersistence()
    {
        TagListResult result = _service.ListTags(new ListTagsRequest());
        _sessionState.NodeTags.Clear();
        foreach (TaggedNodeResult item in result.Items)
        {
            _sessionState.NodeTags[item.Path] = new HashSet<string>(item.Tags.Select(tag => tag.Name), StringComparer.OrdinalIgnoreCase);
        }
    }
}
