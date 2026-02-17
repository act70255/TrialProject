using CloudFileManager.Application.Interfaces;

namespace CloudFileManager.Presentation.ConsoleApp;

/// <summary>
/// ConsoleCommandExecutor 類別，負責執行指令與協調處理流程。
/// </summary>
public sealed partial class ConsoleCommandExecutor : IConsoleCommandExecutor
{
    private readonly ICloudFileApplicationService _service;
    private readonly ILocalFileUploadRequestFactory _localFileUploadRequestFactory;
    private readonly ConsoleSessionState _sessionState;
    private readonly Dictionary<string, Func<IReadOnlyList<string>, bool>> _commandHandlers;

    /// <summary>
    /// 初始化 ConsoleCommandExecutor。
    /// </summary>
    public ConsoleCommandExecutor(
        ICloudFileApplicationService service,
        ILocalFileUploadRequestFactory localFileUploadRequestFactory,
        ConsoleSessionState sessionState)
    {
        _service = service;
        _localFileUploadRequestFactory = localFileUploadRequestFactory;
        _sessionState = sessionState;
        _commandHandlers = BuildCommandHandlers();
    }

    /// <summary>
    /// 目前 目錄路徑。
    /// </summary>
    public string CurrentDirectoryPath => _sessionState.CurrentDirectoryPath;

    /// <summary>
    /// 執行指令。
    /// </summary>
    public bool Execute(ConsoleCommand command)
    {
        if (_commandHandlers.TryGetValue(command.Name, out Func<IReadOnlyList<string>, bool>? handler))
        {
            return handler(command.Arguments);
        }

        PrintError($"Unknown command: {command.Name}");
        ConsoleHelpPrinter.Print();
        return true;
    }

    private Dictionary<string, Func<IReadOnlyList<string>, bool>> BuildCommandHandlers()
    {
        return new Dictionary<string, Func<IReadOnlyList<string>, bool>>(StringComparer.OrdinalIgnoreCase)
        {
            ["help"] = _ =>
            {
                ConsoleHelpPrinter.Print();
                return true;
            },
            ["?"] = _ =>
            {
                ConsoleHelpPrinter.Print();
                return true;
            },
            ["tree"] = _ =>
            {
                PrintTree();
                return true;
            },
            ["ls"] = args => ExecuteAndContinue(HandleList, args),
            ["lsr"] = args => ExecuteAndContinue(HandleListRecursive, args),
            ["cd"] = args => ExecuteAndContinue(HandleChangeDirectory, args),
            ["size"] = args => ExecuteAndContinue(HandleSize, args),
            ["search"] = args => ExecuteAndContinue(HandleSearch, args),
            ["sort"] = args => ExecuteAndContinue(HandleSort, args),
            ["mkdir"] = args => ExecuteAndContinue(HandleCreateDirectory, args),
            ["copy"] = args => ExecuteAndContinue(HandleCopy, args),
            ["paste"] = args => ExecuteAndContinue(HandlePaste, args),
            ["tag"] = args => ExecuteAndContinue(HandleTag, args),
            ["undo"] = _ => ExecuteAndContinue(_ => HandleUndo(), []),
            ["redo"] = _ => ExecuteAndContinue(_ => HandleRedo(), []),
            ["upload"] = args => ExecuteAndContinue(HandleUpload, args),
            ["move-file"] = args => ExecuteAndContinue(HandleMoveFile, args),
            ["rename-file"] = args => ExecuteAndContinue(HandleRenameFile, args),
            ["delete-file"] = args => ExecuteAndContinue(HandleDeleteFile, args),
            ["download"] = args => ExecuteAndContinue(HandleDownload, args),
            ["move-dir"] = args => ExecuteAndContinue(HandleMoveDirectory, args),
            ["rename-dir"] = args => ExecuteAndContinue(HandleRenameDirectory, args),
            ["delete-dir"] = args => ExecuteAndContinue(HandleDeleteDirectory, args),
            ["xml"] = args => ExecuteAndContinue(HandleXml, args),
            ["exit"] = _ => Exit(),
            ["quit"] = _ => Exit()
        };
    }

    private static bool ExecuteAndContinue(Action<IReadOnlyList<string>> action, IReadOnlyList<string> args)
    {
        action(args);
        return true;
    }

    private static bool Exit()
    {
        PrintInfo("Bye.");
        return false;
    }

    private string ResolveDirectoryPath(string rawPath)
    {
        return ConsolePathResolver.Resolve(rawPath, CurrentDirectoryPath);
    }

    private string ResolveFilePath(string rawPath)
    {
        return ConsolePathResolver.Resolve(rawPath, CurrentDirectoryPath);
    }

    private static void PrintResult(bool success, string message)
    {
        System.Console.WriteLine(success ? $"[RESULT] OK: {message}" : $"[RESULT] FAIL: {message}");
    }

    private static void PrintInfo(string message)
    {
        System.Console.WriteLine($"[INFO] {message}");
    }

    private static void PrintError(string message)
    {
        System.Console.WriteLine($"[ERROR] {message}");
    }

    private static void PrintUsage(string usage)
    {
        System.Console.WriteLine($"[USAGE] {usage}");
    }

    private static void PrintSectionHeader(string title)
    {
        System.Console.WriteLine($"<<{title}>>");
    }

    private static void PrintSectionFooter(string title)
    {
        System.Console.WriteLine("----------");
    }
}
