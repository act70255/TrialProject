using CloudFileManager.Application.Interfaces;
using CloudFileManager.Application.Models;

namespace CloudFileManager.Application.Implementations;

/// <summary>
/// CloudFileApplicationService 類別，負責協調應用層流程與業務操作。
/// </summary>
public sealed class CloudFileApplicationService : ICloudFileApplicationService
{
    private readonly ICloudFileReadModelService _readModelService;
    private readonly ICloudFileFileCommandService _fileCommandService;
    private readonly ICloudFileDirectoryCommandService _directoryCommandService;

    public CloudFileApplicationService(
        ICloudFileReadModelService readModelService,
        ICloudFileFileCommandService fileCommandService,
        ICloudFileDirectoryCommandService directoryCommandService)
    {
        _readModelService = readModelService;
        _fileCommandService = fileCommandService;
        _directoryCommandService = directoryCommandService;
    }

    /// <summary>
    /// 取得目錄樹資料。
    /// </summary>
    public DirectoryTreeResult GetDirectoryTree()
    {
        return _readModelService.GetDirectoryTree();
    }

    /// <summary>
    /// 建立目錄。
    /// </summary>
    public OperationResult CreateDirectory(CreateDirectoryRequest request)
    {
        return _directoryCommandService.CreateDirectory(request);
    }

    public Task<OperationResult> CreateDirectoryAsync(CreateDirectoryRequest request, CancellationToken cancellationToken = default)
    {
        return _directoryCommandService.CreateDirectoryAsync(request, cancellationToken);
    }

    /// <summary>
    /// 上傳檔案。
    /// </summary>
    public OperationResult UploadFile(UploadFileRequest request)
    {
        return _fileCommandService.UploadFile(request);
    }

    public Task<OperationResult> UploadFileAsync(UploadFileRequest request, CancellationToken cancellationToken = default)
    {
        return _fileCommandService.UploadFileAsync(request, cancellationToken);
    }

    /// <summary>
    /// 下載檔案。
    /// </summary>
    public OperationResult DownloadFile(DownloadFileRequest request)
    {
        return _fileCommandService.DownloadFile(request);
    }

    public Task<OperationResult> DownloadFileAsync(DownloadFileRequest request, CancellationToken cancellationToken = default)
    {
        return _fileCommandService.DownloadFileAsync(request, cancellationToken);
    }

    /// <summary>
    /// 下載檔案內容。
    /// </summary>
    public FileDownloadResult DownloadFileContent(string filePath)
    {
        return _fileCommandService.DownloadFileContent(filePath);
    }

    public Task<FileDownloadResult> DownloadFileContentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return _fileCommandService.DownloadFileContentAsync(filePath, cancellationToken);
    }

    /// <summary>
    /// 搬移檔案。
    /// </summary>
    public OperationResult MoveFile(MoveFileRequest request)
    {
        return _fileCommandService.MoveFile(request);
    }

    public Task<OperationResult> MoveFileAsync(MoveFileRequest request, CancellationToken cancellationToken = default)
    {
        return _fileCommandService.MoveFileAsync(request, cancellationToken);
    }

    /// <summary>
    /// 重新命名檔案。
    /// </summary>
    public OperationResult RenameFile(RenameFileRequest request)
    {
        return _fileCommandService.RenameFile(request);
    }

    public Task<OperationResult> RenameFileAsync(RenameFileRequest request, CancellationToken cancellationToken = default)
    {
        return _fileCommandService.RenameFileAsync(request, cancellationToken);
    }

    /// <summary>
    /// 刪除檔案。
    /// </summary>
    public OperationResult DeleteFile(DeleteFileRequest request)
    {
        return _fileCommandService.DeleteFile(request);
    }

    public Task<OperationResult> DeleteFileAsync(DeleteFileRequest request, CancellationToken cancellationToken = default)
    {
        return _fileCommandService.DeleteFileAsync(request, cancellationToken);
    }

    /// <summary>
    /// 刪除目錄。
    /// </summary>
    public OperationResult DeleteDirectory(DeleteDirectoryRequest request)
    {
        return _directoryCommandService.DeleteDirectory(request);
    }

    public Task<OperationResult> DeleteDirectoryAsync(DeleteDirectoryRequest request, CancellationToken cancellationToken = default)
    {
        return _directoryCommandService.DeleteDirectoryAsync(request, cancellationToken);
    }

    /// <summary>
    /// 搬移目錄。
    /// </summary>
    public OperationResult MoveDirectory(MoveDirectoryRequest request)
    {
        return _directoryCommandService.MoveDirectory(request);
    }

    public Task<OperationResult> MoveDirectoryAsync(MoveDirectoryRequest request, CancellationToken cancellationToken = default)
    {
        return _directoryCommandService.MoveDirectoryAsync(request, cancellationToken);
    }

    /// <summary>
    /// 重新命名目錄。
    /// </summary>
    public OperationResult RenameDirectory(RenameDirectoryRequest request)
    {
        return _directoryCommandService.RenameDirectory(request);
    }

    public Task<OperationResult> RenameDirectoryAsync(RenameDirectoryRequest request, CancellationToken cancellationToken = default)
    {
        return _directoryCommandService.RenameDirectoryAsync(request, cancellationToken);
    }

    /// <summary>
    /// 計算目錄總容量。
    /// </summary>
    public SizeCalculationResult CalculateTotalSize(CalculateSizeRequest request)
    {
        return _readModelService.CalculateTotalSize(request);
    }

    /// <summary>
    /// 依副檔名搜尋檔案。
    /// </summary>
    public SearchResult SearchByExtension(SearchByExtensionRequest request)
    {
        return _readModelService.SearchByExtension(request);
    }

    public DirectoryEntriesResult GetDirectoryEntries(ListDirectoryEntriesRequest request)
    {
        return _readModelService.GetDirectoryEntries(request);
    }

    /// <summary>
    /// 匯出目錄樹 XML。
    /// </summary>
    public XmlExportResult ExportXml(ExportXmlRequest? request = null)
    {
        return _readModelService.ExportXml(request);
    }

    public OperationResult CopyFile(CopyFileRequest request)
    {
        return _fileCommandService.CopyFile(request);
    }

    public Task<OperationResult> CopyFileAsync(CopyFileRequest request, CancellationToken cancellationToken = default)
    {
        return _fileCommandService.CopyFileAsync(request, cancellationToken);
    }

    public OperationResult CopyDirectory(CopyDirectoryRequest request)
    {
        return _directoryCommandService.CopyDirectory(request);
    }

    public Task<OperationResult> CopyDirectoryAsync(CopyDirectoryRequest request, CancellationToken cancellationToken = default)
    {
        return _directoryCommandService.CopyDirectoryAsync(request, cancellationToken);
    }

    /// <summary>
    /// 取得功能旗標設定。
    /// </summary>
    public FeatureFlagsResult GetFeatureFlags()
    {
        return _readModelService.GetFeatureFlags();
    }

}
