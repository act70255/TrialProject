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
        System.Console.WriteLine("  cd <directoryPath>                            切換目前工作目錄");
        System.Console.WriteLine("  pwd                                           顯示目前工作目錄");
        System.Console.WriteLine("  size [directoryPath]                          計算目錄大小，未提供路徑則使用目前目錄");
        System.Console.WriteLine("  search <extension>                            依副檔名搜尋檔案 (例如 .txt)");
        System.Console.WriteLine("  mkdir <directoryName> OR mkdir <parentPath> <directoryName>  建立新目錄");
        System.Console.WriteLine("  upload <localFilePath> OR upload <directoryPath> <localFilePath>  上傳本機檔案");
        System.Console.WriteLine("  move-file <sourceFilePath> <targetDirectoryPath>              移動檔案到指定目錄");
        System.Console.WriteLine("  rename-file <filePath> <newFileName>                           重新命名檔案");
        System.Console.WriteLine("  delete-file <filePath>                                         刪除檔案");
        System.Console.WriteLine("  download <filePath> <targetLocalPath>                          下載檔案到本機路徑");
        System.Console.WriteLine("  move-dir <sourceDirectoryPath> <targetParentDirectoryPath>     移動目錄到指定父目錄");
        System.Console.WriteLine("  rename-dir <directoryPath> <newDirectoryName>                  重新命名目錄");
        System.Console.WriteLine("  delete-dir <directoryPath>                                      刪除目錄");
        System.Console.WriteLine("  xml                                           匯出目前目錄結構為 XML");
        System.Console.WriteLine("  exit                                          離開程式");
    }
}
