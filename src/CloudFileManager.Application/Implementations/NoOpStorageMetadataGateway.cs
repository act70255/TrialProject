using CloudFileManager.Application.Interfaces;
using CloudFileManager.Application.Models;
using CloudFileManager.Domain;
using CloudFileManager.Domain.Enums;

namespace CloudFileManager.Application.Implementations;

/// <summary>
/// NoOpStorageMetadataGateway 類別，負責提供無持久化情境的替代實作。
/// </summary>
public sealed class NoOpStorageMetadataGateway : IStorageMetadataGateway
{
    public static NoOpStorageMetadataGateway Instance { get; } = new();

    private NoOpStorageMetadataGateway()
    {
    }

    public CloudDirectory LoadRootTree()
    {
        return new CloudDirectory("Root", DateTime.UtcNow);
    }

    public OperationResult CreateDirectory(string parentPath, string directoryName)
    {
        return new OperationResult(true, "Skipped persistence.");
    }

    public Task<OperationResult> CreateDirectoryAsync(string parentPath, string directoryName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDirectory(parentPath, directoryName));
    }

    public OperationResult UploadFile(UploadFileRequest request, CloudFileType fileType)
    {
        return new OperationResult(true, "Skipped persistence.");
    }

    public Task<OperationResult> UploadFileAsync(UploadFileRequest request, CloudFileType fileType, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(UploadFile(request, fileType));
    }

    public OperationResult MoveFile(string sourceFilePath, string targetDirectoryPath)
    {
        return new OperationResult(true, "Skipped persistence.");
    }

    public Task<OperationResult> MoveFileAsync(string sourceFilePath, string targetDirectoryPath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MoveFile(sourceFilePath, targetDirectoryPath));
    }

    public OperationResult RenameFile(string filePath, string newFileName)
    {
        return new OperationResult(true, "Skipped persistence.");
    }

    public Task<OperationResult> RenameFileAsync(string filePath, string newFileName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RenameFile(filePath, newFileName));
    }

    public OperationResult DeleteFile(string filePath)
    {
        return new OperationResult(true, "Skipped persistence.");
    }

    public Task<OperationResult> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DeleteFile(filePath));
    }

    public OperationResult DownloadFile(string filePath, string targetLocalPath)
    {
        return new OperationResult(false, "Download requires storage gateway.");
    }

    public Task<OperationResult> DownloadFileAsync(string filePath, string targetLocalPath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DownloadFile(filePath, targetLocalPath));
    }

    public FileDownloadResult DownloadFileContent(string filePath)
    {
        return new FileDownloadResult(false, "Download requires storage gateway.", string.Empty, null, "application/octet-stream");
    }

    public Task<FileDownloadResult> DownloadFileContentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DownloadFileContent(filePath));
    }

    public OperationResult DeleteDirectory(string directoryPath)
    {
        return new OperationResult(true, "Skipped persistence.");
    }

    public Task<OperationResult> DeleteDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DeleteDirectory(directoryPath));
    }

    public OperationResult MoveDirectory(string sourceDirectoryPath, string targetParentDirectoryPath)
    {
        return new OperationResult(true, "Skipped persistence.");
    }

    public Task<OperationResult> MoveDirectoryAsync(string sourceDirectoryPath, string targetParentDirectoryPath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MoveDirectory(sourceDirectoryPath, targetParentDirectoryPath));
    }

    public OperationResult RenameDirectory(string directoryPath, string newDirectoryName)
    {
        return new OperationResult(true, "Skipped persistence.");
    }

    public Task<OperationResult> RenameDirectoryAsync(string directoryPath, string newDirectoryName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RenameDirectory(directoryPath, newDirectoryName));
    }
}
