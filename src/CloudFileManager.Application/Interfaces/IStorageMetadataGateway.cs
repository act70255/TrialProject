using CloudFileManager.Application.Models;
using CloudFileManager.Domain;
using CloudFileManager.Domain.Enums;

namespace CloudFileManager.Application.Interfaces;

/// <summary>
/// IStorageMetadataGateway 介面契約。
/// </summary>
public interface IStorageMetadataGateway
{
    /// <summary>
    /// 載入根目錄樹。
    /// </summary>
    CloudDirectory LoadRootTree();

    /// <summary>
    /// 建立目錄。
    /// </summary>
    OperationResult CreateDirectory(string parentPath, string directoryName);

    /// <summary>
    /// 以非同步方式建立目錄。
    /// </summary>
    Task<OperationResult> CreateDirectoryAsync(string parentPath, string directoryName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 上傳檔案。
    /// </summary>
    OperationResult UploadFile(UploadFileRequest request, CloudFileType fileType);

    /// <summary>
    /// 以非同步方式上傳檔案。
    /// </summary>
    Task<OperationResult> UploadFileAsync(UploadFileRequest request, CloudFileType fileType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搬移檔案。
    /// </summary>
    OperationResult MoveFile(string sourceFilePath, string targetDirectoryPath);

    /// <summary>
    /// 以非同步方式搬移檔案。
    /// </summary>
    Task<OperationResult> MoveFileAsync(string sourceFilePath, string targetDirectoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重新命名檔案。
    /// </summary>
    OperationResult RenameFile(string filePath, string newFileName);

    /// <summary>
    /// 以非同步方式重新命名檔案。
    /// </summary>
    Task<OperationResult> RenameFileAsync(string filePath, string newFileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刪除檔案。
    /// </summary>
    OperationResult DeleteFile(string filePath);

    /// <summary>
    /// 以非同步方式刪除檔案。
    /// </summary>
    Task<OperationResult> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下載檔案。
    /// </summary>
    OperationResult DownloadFile(string filePath, string targetLocalPath);

    /// <summary>
    /// 以非同步方式下載檔案。
    /// </summary>
    Task<OperationResult> DownloadFileAsync(string filePath, string targetLocalPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下載檔案內容。
    /// </summary>
    FileDownloadResult DownloadFileContent(string filePath);

    /// <summary>
    /// 以非同步方式下載檔案內容。
    /// </summary>
    Task<FileDownloadResult> DownloadFileContentAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刪除目錄。
    /// </summary>
    OperationResult DeleteDirectory(string directoryPath);

    /// <summary>
    /// 以非同步方式刪除目錄。
    /// </summary>
    Task<OperationResult> DeleteDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搬移目錄。
    /// </summary>
    OperationResult MoveDirectory(string sourceDirectoryPath, string targetParentDirectoryPath);

    /// <summary>
    /// 以非同步方式搬移目錄。
    /// </summary>
    Task<OperationResult> MoveDirectoryAsync(string sourceDirectoryPath, string targetParentDirectoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重新命名目錄。
    /// </summary>
    OperationResult RenameDirectory(string directoryPath, string newDirectoryName);

    /// <summary>
    /// 以非同步方式重新命名目錄。
    /// </summary>
    Task<OperationResult> RenameDirectoryAsync(string directoryPath, string newDirectoryName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 指派標籤到節點。
    /// </summary>
    OperationResult AssignTag(string path, string tagName);

    /// <summary>
    /// 移除節點標籤。
    /// </summary>
    OperationResult RemoveTag(string path, string tagName);

    /// <summary>
    /// 查詢標籤列表。
    /// </summary>
    TagListResult ListTags(string? path);

    /// <summary>
    /// 依標籤查詢節點。
    /// </summary>
    TagFindResult FindTaggedPaths(string tagName, string scopePath);
}
