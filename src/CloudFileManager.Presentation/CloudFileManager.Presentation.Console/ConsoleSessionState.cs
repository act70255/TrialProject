namespace CloudFileManager.Presentation.ConsoleApp;

/// <summary>
/// ConsoleSessionState 類別，負責保存互動式命令列會話狀態。
/// </summary>
public sealed class ConsoleSessionState
{
    /// <summary>
    /// 目前目錄路徑。
    /// </summary>
    public string CurrentDirectoryPath { get; set; } = "Root";
}
