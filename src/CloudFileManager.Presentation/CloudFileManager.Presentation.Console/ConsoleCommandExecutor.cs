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

        System.Console.WriteLine($"Unknown command: {command.Name}");
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
            ["cd"] = args => ExecuteAndContinue(HandleChangeDirectory, args),
            ["pwd"] = _ =>
            {
                System.Console.WriteLine(CurrentDirectoryPath);
                return true;
            },
            ["size"] = args => ExecuteAndContinue(HandleSize, args),
            ["search"] = args => ExecuteAndContinue(HandleSearch, args),
            ["mkdir"] = args => ExecuteAndContinue(HandleCreateDirectory, args),
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
        System.Console.WriteLine("Bye.");
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
        System.Console.WriteLine(success ? $"OK: {message}" : $"FAIL: {message}");
    }
}
