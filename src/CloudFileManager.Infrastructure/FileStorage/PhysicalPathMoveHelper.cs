namespace CloudFileManager.Infrastructure.FileStorage;

/// <summary>
/// PhysicalPathMoveHelper 類別，負責處理檔案系統搬移與大小寫重命名。
/// </summary>
public static class PhysicalPathMoveHelper
{
    /// <summary>
    /// 搬移檔案，支援僅大小寫變更的重命名。
    /// </summary>
    public static void MoveFile(string sourcePath, string destinationPath, bool caseOnlyRename)
    {
        if (!caseOnlyRename)
        {
            File.Move(sourcePath, destinationPath);
            return;
        }

        string parentPath = Path.GetDirectoryName(sourcePath)
            ?? throw new InvalidOperationException("File parent path cannot be resolved.");
        string temporaryPath = Path.Combine(parentPath, $".__case_rename_{Guid.NewGuid():N}{Path.GetExtension(destinationPath)}");
        File.Move(sourcePath, temporaryPath);
        File.Move(temporaryPath, destinationPath);
    }

    /// <summary>
    /// 搬移目錄，支援僅大小寫變更的重命名。
    /// </summary>
    public static void MoveDirectory(string sourcePath, string destinationPath, bool caseOnlyRename)
    {
        if (!caseOnlyRename)
        {
            Directory.Move(sourcePath, destinationPath);
            return;
        }

        string parentPath = Path.GetDirectoryName(sourcePath)
            ?? throw new InvalidOperationException("Directory parent path cannot be resolved.");
        string temporaryPath = Path.Combine(parentPath, $".__case_rename_{Guid.NewGuid():N}");
        Directory.Move(sourcePath, temporaryPath);
        Directory.Move(temporaryPath, destinationPath);
    }
}
