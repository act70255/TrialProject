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

    /// <summary>
    /// 剪貼簿內容。
    /// </summary>
    public ConsoleClipboardItem? ClipboardItem { get; set; }

    /// <summary>
    /// 節點標籤對照表（Key: 節點完整路徑）。
    /// </summary>
    public Dictionary<string, HashSet<string>> NodeTags { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 目前排序設定。
    /// </summary>
    public ConsoleSortState? CurrentSortState { get; set; }

    /// <summary>
    /// Undo 堆疊。
    /// </summary>
    public Stack<ConsoleUndoAction> UndoStack { get; } = new();

    /// <summary>
    /// Redo 堆疊。
    /// </summary>
    public Stack<ConsoleUndoAction> RedoStack { get; } = new();

}

public sealed record ConsoleClipboardItem(bool IsDirectory, string Path);

public sealed record ConsoleSortState(string Key, string Direction);

public enum ConsoleUndoActionKind
{
    SortSettingChanged,
    TagAdded,
    TagRemoved
}

public sealed record ConsoleUndoAction(
    ConsoleUndoActionKind Kind,
    ConsoleSortState? PreviousSortState = null,
    ConsoleSortState? CurrentSortState = null,
    string? NodePath = null,
    string? TagName = null,
    string? TagColor = null);
