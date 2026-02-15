namespace CloudFileManager.Presentation.ConsoleApp;

/// <summary>
/// ConsoleCommand 類別，負責描述命令資料與操作意圖。
/// </summary>
public sealed class ConsoleCommand
{
    /// <summary>
    /// 初始化 ConsoleCommand。
    /// </summary>
    public ConsoleCommand()
    {
        Name = string.Empty;
        Arguments = Array.Empty<string>();
    }

    /// <summary>
    /// 初始化 ConsoleCommand。
    /// </summary>
    public ConsoleCommand(string name, IReadOnlyList<string> arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    /// <summary>
    /// 取得或設定 名稱。
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 取得或設定 Arguments。
    /// </summary>
    public IReadOnlyList<string> Arguments { get; set; }
}
