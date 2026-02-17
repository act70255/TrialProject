using CloudFileManager.Application.Models;

namespace CloudFileManager.Application.Interfaces;

public interface ICloudFileFileCommandService
{
    OperationResult UploadFile(UploadFileRequest request);

    Task<OperationResult> UploadFileAsync(UploadFileRequest request, CancellationToken cancellationToken = default);

    OperationResult DownloadFile(DownloadFileRequest request);

    Task<OperationResult> DownloadFileAsync(DownloadFileRequest request, CancellationToken cancellationToken = default);

    FileDownloadResult DownloadFileContent(string filePath);

    Task<FileDownloadResult> DownloadFileContentAsync(string filePath, CancellationToken cancellationToken = default);

    OperationResult MoveFile(MoveFileRequest request);

    Task<OperationResult> MoveFileAsync(MoveFileRequest request, CancellationToken cancellationToken = default);

    OperationResult RenameFile(RenameFileRequest request);

    Task<OperationResult> RenameFileAsync(RenameFileRequest request, CancellationToken cancellationToken = default);

    OperationResult DeleteFile(DeleteFileRequest request);

    OperationResult CopyFile(CopyFileRequest request);

    Task<OperationResult> DeleteFileAsync(DeleteFileRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult> CopyFileAsync(CopyFileRequest request, CancellationToken cancellationToken = default);
}
