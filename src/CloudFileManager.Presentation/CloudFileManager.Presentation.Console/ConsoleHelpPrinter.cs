namespace CloudFileManager.Presentation.ConsoleApp;

/// <summary>
/// ConsoleHelpPrinter 類別，負責輸出可用指令清單。
/// </summary>
public static class ConsoleHelpPrinter
{
    /// <summary>
    /// 輸出說明。
    /// </summary>
    public static void Print()
    {
        System.Console.WriteLine("Commands:");
        System.Console.WriteLine("  help                                          顯示指令說明");
        System.Console.WriteLine("  ?                                             顯示指令說明 (help 別名)");
        System.Console.WriteLine("  tree                                          顯示完整目錄樹");
        System.Console.WriteLine("  ls [directoryPath]                            列出目錄內容，未提供路徑則使用目前目錄");
        System.Console.WriteLine("  lsr [directoryPath]                           遞迴列出目錄以下全部子孫節點");
        System.Console.WriteLine("  cd <directoryPath>                            切換目前工作目錄");
        System.Console.WriteLine("  size [directoryPath]                          計算目錄大小，未提供路徑則使用目前目錄");
        System.Console.WriteLine("  search <extension> [directoryPath]            依副檔名搜尋檔案，未提供路徑則使用目前目錄");
        System.Console.WriteLine("  sort <name|size|ext> <asc|desc>                 設定全域排序規則（供 lsr 套用）");
        System.Console.WriteLine("  mkdir <directoryName> OR mkdir <parentPath> <directoryName>  建立新目錄");
        System.Console.WriteLine("  copy <sourcePath>                             複製檔案或目錄到剪貼簿");
        System.Console.WriteLine("  paste [targetDirectoryPath]                   貼上剪貼簿到指定目錄，未提供路徑則使用目前目錄");
        System.Console.WriteLine("  tag <path> <Urgent|Work|Personal>             套用標籤到節點（可多重）");
        System.Console.WriteLine("  tag remove <path> <Urgent|Work|Personal>      移除節點標籤");
        System.Console.WriteLine("  tag list [path]                               顯示節點標籤或所有已標記節點");
        System.Console.WriteLine("  tag find <Urgent|Work|Personal> [directoryPath]  依標籤查詢節點");
        System.Console.WriteLine("  undo                                          復原上一個排序設定或標籤增刪操作");
        System.Console.WriteLine("  redo                                          重做上一個被復原的排序設定或標籤增刪操作");
        System.Console.WriteLine("  upload <localFilePath> OR upload <directoryPath> <localFilePath>  上傳本機檔案");
        System.Console.WriteLine("  move-file <sourceFilePath> <targetDirectoryPath>              移動檔案到指定目錄");
        System.Console.WriteLine("  rename-file <filePath> <newFileName>                           重新命名檔案");
        System.Console.WriteLine("  delete-file <filePath>                                         刪除檔案");
        System.Console.WriteLine("  download <filePath> <targetLocalPath>                          下載檔案到本機路徑");
        System.Console.WriteLine("  move-dir <sourceDirectoryPath> <targetParentDirectoryPath>     移動目錄到指定父目錄");
        System.Console.WriteLine("  rename-dir <directoryPath> <newDirectoryName>                  重新命名目錄");
        System.Console.WriteLine("  delete-dir <directoryPath>                                      刪除目錄");
        System.Console.WriteLine("  xml [directoryPath] [raw]                     匯出目錄結構 XML（預設題目樣式；加 raw 顯示屬性格式）");
        System.Console.WriteLine("  exit                                          離開程式");
    }
}
