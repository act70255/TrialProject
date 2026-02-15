namespace CloudFileManager.Presentation.ConsoleApp;

/// <summary>
/// IConsoleCommandExecutor 介面契約。
/// </summary>
public interface IConsoleCommandExecutor
{
    /// <summary>
    /// 取得或設定目前 目錄路徑。
    /// </summary>
    string CurrentDirectoryPath { get; }

    /// <summary>
    /// 執行指令。
    /// </summary>
    bool Execute(ConsoleCommand command);
}
