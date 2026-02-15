namespace CloudFileManager.Presentation.ConsoleApp;

/// <summary>
/// IConsoleCommandParser 介面契約。
/// </summary>
public interface IConsoleCommandParser
{
    /// <summary>
    /// 解析資料。
    /// </summary>
    ConsoleCommand Parse(string input);
}
