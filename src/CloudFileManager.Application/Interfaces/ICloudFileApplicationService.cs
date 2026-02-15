using CloudFileManager.Application.Models;

namespace CloudFileManager.Application.Interfaces;

/// <summary>
/// ICloudFileApplicationService 介面契約。
/// </summary>
public interface ICloudFileApplicationService
{
    /// <summary>
    /// 取得目錄樹資料。
    /// </summary>
    DirectoryTreeResult GetDirectoryTree();

    /// <summary>
    /// 建立目錄。
    /// </summary>
    OperationResult CreateDirectory(CreateDirectoryRequest request);

    Task<OperationResult> CreateDirectoryAsync(CreateDirectoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 上傳檔案。
    /// </summary>
    OperationResult UploadFile(UploadFileRequest request);

    Task<OperationResult> UploadFileAsync(UploadFileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搬移檔案。
    /// </summary>
    OperationResult MoveFile(MoveFileRequest request);

    Task<OperationResult> MoveFileAsync(MoveFileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重新命名檔案。
    /// </summary>
    OperationResult RenameFile(RenameFileRequest request);

    Task<OperationResult> RenameFileAsync(RenameFileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刪除檔案。
    /// </summary>
    OperationResult DeleteFile(DeleteFileRequest request);

    Task<OperationResult> DeleteFileAsync(DeleteFileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下載檔案。
    /// </summary>
    OperationResult DownloadFile(DownloadFileRequest request);

    Task<OperationResult> DownloadFileAsync(DownloadFileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下載檔案內容。
    /// </summary>
    FileDownloadResult DownloadFileContent(string filePath);

    Task<FileDownloadResult> DownloadFileContentAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刪除目錄。
    /// </summary>
    OperationResult DeleteDirectory(DeleteDirectoryRequest request);

    Task<OperationResult> DeleteDirectoryAsync(DeleteDirectoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搬移目錄。
    /// </summary>
    OperationResult MoveDirectory(MoveDirectoryRequest request);

    Task<OperationResult> MoveDirectoryAsync(MoveDirectoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重新命名目錄。
    /// </summary>
    OperationResult RenameDirectory(RenameDirectoryRequest request);

    Task<OperationResult> RenameDirectoryAsync(RenameDirectoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 計算目錄總容量。
    /// </summary>
    SizeCalculationResult CalculateTotalSize(CalculateSizeRequest request);

    /// <summary>
    /// 依副檔名搜尋檔案。
    /// </summary>
    SearchResult SearchByExtension(SearchByExtensionRequest request);

    /// <summary>
    /// 匯出目錄樹 XML。
    /// </summary>
    XmlExportResult ExportXml();

    /// <summary>
    /// 取得功能旗標設定。
    /// </summary>
    FeatureFlagsResult GetFeatureFlags();
}
