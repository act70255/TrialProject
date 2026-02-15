using CloudFileManager.Application.Models;

namespace CloudFileManager.Presentation.ConsoleApp;

public sealed partial class ConsoleCommandExecutor
{
    private void HandleChangeDirectory(IReadOnlyList<string> args)
    {
        if (args.Count < 1)
        {
            System.Console.WriteLine("Usage: cd <directoryPath>");
            return;
        }

        string targetPath = ResolveDirectoryPath(args[0]);
        ConsoleDirectorySnapshot snapshot = ConsoleDirectorySnapshotBuilder.Build(_service.GetDirectoryTree());
        if (!snapshot.DirectoryPaths.Contains(targetPath))
        {
            System.Console.WriteLine($"Directory not found: {targetPath}");
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
            System.Console.WriteLine($"Directory not found: {targetPath}");
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
            System.Console.WriteLine("(empty)");
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
            System.Console.WriteLine("Usage: mkdir <directoryName> OR mkdir <parentPath> <directoryName>");
            return;
        }

        string parentPath = args.Count == 1 ? CurrentDirectoryPath : ResolveDirectoryPath(args[0]);
        string directoryName = args.Count == 1 ? args[0] : args[1];

        OperationResult result = _service.CreateDirectory(new CreateDirectoryRequest(parentPath, directoryName));
        PrintResult(result.Success, result.Message);
    }

    private void HandleUpload(IReadOnlyList<string> args)
    {
        if (args.Count == 0 || args.Count > 2)
        {
            System.Console.WriteLine("Usage: upload <localFilePath> OR upload <directoryPath> <localFilePath>");
            return;
        }

        string targetDirectoryPath = args.Count == 1 ? CurrentDirectoryPath : ResolveDirectoryPath(args[0]);
        string localPathInput = args.Count == 1 ? args[0] : args[1];
        string localPath = Path.GetFullPath(localPathInput);

        if (!File.Exists(localPath))
        {
            System.Console.WriteLine($"Local file not found: {localPath}");
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
            System.Console.WriteLine($"FAIL: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Console.WriteLine($"FAIL: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine($"FAIL: {ex.Message}");
        }
        catch
        {
            System.Console.WriteLine("FAIL: Upload failed due to an unexpected error.");
        }
    }

    private void HandleMoveFile(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
        {
            System.Console.WriteLine("Usage: move-file <sourceFilePath> <targetDirectoryPath>");
            return;
        }

        OperationResult result = _service.MoveFile(new MoveFileRequest(ResolveFilePath(args[0]), ResolveDirectoryPath(args[1])));
        PrintResult(result.Success, result.Message);
    }

    private void HandleRenameFile(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
        {
            System.Console.WriteLine("Usage: rename-file <filePath> <newFileName>");
            return;
        }

        OperationResult result = _service.RenameFile(new RenameFileRequest(ResolveFilePath(args[0]), args[1]));
        PrintResult(result.Success, result.Message);
    }

    private void HandleDeleteFile(IReadOnlyList<string> args)
    {
        if (args.Count < 1)
        {
            System.Console.WriteLine("Usage: delete-file <filePath>");
            return;
        }

        OperationResult result = _service.DeleteFile(new DeleteFileRequest(ResolveFilePath(args[0])));
        PrintResult(result.Success, result.Message);
    }

    private void HandleDownload(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
        {
            System.Console.WriteLine("Usage: download <filePath> <targetLocalPath>");
            return;
        }

        OperationResult result = _service.DownloadFile(new DownloadFileRequest(ResolveFilePath(args[0]), args[1]));
        PrintResult(result.Success, result.Message);
    }

    private void HandleMoveDirectory(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
        {
            System.Console.WriteLine("Usage: move-dir <sourceDirectoryPath> <targetParentDirectoryPath>");
            return;
        }

        OperationResult result = _service.MoveDirectory(new MoveDirectoryRequest(ResolveDirectoryPath(args[0]), ResolveDirectoryPath(args[1])));
        PrintResult(result.Success, result.Message);
    }

    private void HandleRenameDirectory(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
        {
            System.Console.WriteLine("Usage: rename-dir <directoryPath> <newDirectoryName>");
            return;
        }

        OperationResult result = _service.RenameDirectory(new RenameDirectoryRequest(ResolveDirectoryPath(args[0]), args[1]));
        PrintResult(result.Success, result.Message);
    }

    private void HandleDeleteDirectory(IReadOnlyList<string> args)
    {
        if (args.Count < 1)
        {
            System.Console.WriteLine("Usage: delete-dir <directoryPath>");
            return;
        }

        OperationResult result = _service.DeleteDirectory(new DeleteDirectoryRequest(ResolveDirectoryPath(args[0])));
        PrintResult(result.Success, result.Message);
    }
}
