using CloudFileManager.Application.Models;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using CloudFileManager.Shared.Common;
using Microsoft.Extensions.Logging;

namespace CloudFileManager.Infrastructure.FileStorage;

public sealed partial class StorageMetadataGateway
{
    private static readonly Action<ILogger, string, Exception?> LogDownloadFileFailedMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1301, "DownloadFileFailed"), "Download file failed for {FilePath}");
    private static readonly Action<ILogger, string, Exception?> LogDownloadFileAsyncFailedMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1302, "DownloadFileAsyncFailed"), "Download file async failed for {FilePath}");
    private static readonly Action<ILogger, string, Exception?> LogDownloadFileContentFailedMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1303, "DownloadFileContentFailed"), "Download file content failed for {FilePath}");
    private static readonly Action<ILogger, string, Exception?> LogDownloadFileContentAsyncFailedMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1304, "DownloadFileContentAsyncFailed"), "Download file content async failed for {FilePath}");

    public OperationResult DeleteFile(string filePath)
    {
        var sourceLookup = StoragePathLookupQueries.FindFileByPath(_dbContext, filePath);
        if (sourceLookup.File is null)
        {
            return new OperationResult(false, $"File not found: {filePath}");
        }

        string sourcePath = ResolvePhysicalPath(sourceLookup.File.RelativePath);
        string backupPath = BuildPendingReplacePath(sourcePath);
        if (!TryMoveFileIfExists(sourcePath, backupPath, out string? moveErrorMessage))
        {
            _auditTrailWriter.Write($"DELETE_FILE|{filePath}|FAIL|{moveErrorMessage}");
            return new OperationResult(false, moveErrorMessage ?? "Unable to prepare file deletion.", OperationErrorCodes.DeleteFileUnexpected);
        }

        FileMetadataEntity? metadata = _dbContext.FileMetadata.FirstOrDefault(item => item.FileId == sourceLookup.File.Id);
        if (metadata is not null)
        {
            _dbContext.FileMetadata.Remove(metadata);
        }

        _dbContext.Files.Remove(sourceLookup.File);
        OperationResult persistenceResult = ExecuteSaveWithRollback(
            restoreEntityState: () =>
            {
                if (metadata is not null)
                {
                    _dbContext.Entry(metadata).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
                }

                _dbContext.Entry(sourceLookup.File).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
            },
            rollbackPhysicalState: () => RestoreMovedFileIfAvailable(backupPath, sourcePath),
            successAuditEntry: $"DELETE_FILE|{filePath}|SUCCESS",
            failureAuditPrefix: $"DELETE_FILE|{filePath}|FAIL",
            successMessage: "File deleted from storage and database.",
            rollbackFailureMessage: "Database update failed after physical delete. The operation was rolled back.",
            rollbackFailureErrorCode: OperationErrorCodes.DeleteFileUnexpected);

        if (!persistenceResult.Success)
        {
            return persistenceResult;
        }

        if (!TryDeleteFileIfExists(backupPath))
        {
            _auditTrailWriter.Write($"DELETE_FILE|{filePath}|CLEANUP_FAIL|{backupPath}");
            return new OperationResult(false, "File metadata was deleted, but physical cleanup did not complete.", OperationErrorCodes.DeleteFileCleanupFailed);
        }

        return persistenceResult;
    }

    public Task<OperationResult> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return DeleteFileCoreAsync(filePath, cancellationToken);
    }

    private async Task<OperationResult> DeleteFileCoreAsync(string filePath, CancellationToken cancellationToken)
    {
        var sourceLookup = StoragePathLookupQueries.FindFileByPath(_dbContext, filePath);
        if (sourceLookup.File is null)
        {
            return new OperationResult(false, $"File not found: {filePath}");
        }

        string sourcePath = ResolvePhysicalPath(sourceLookup.File.RelativePath);
        string backupPath = BuildPendingReplacePath(sourcePath);
        if (!TryMoveFileIfExists(sourcePath, backupPath, out string? moveErrorMessage))
        {
            _auditTrailWriter.Write($"DELETE_FILE|{filePath}|FAIL|{moveErrorMessage}");
            return new OperationResult(false, moveErrorMessage ?? "Unable to prepare file deletion.", OperationErrorCodes.DeleteFileUnexpected);
        }

        FileMetadataEntity? metadata = _dbContext.FileMetadata.FirstOrDefault(item => item.FileId == sourceLookup.File.Id);
        if (metadata is not null)
        {
            _dbContext.FileMetadata.Remove(metadata);
        }

        _dbContext.Files.Remove(sourceLookup.File);
        OperationResult persistenceResult = await ExecuteSaveWithRollbackAsync(
            restoreEntityState: () =>
            {
                if (metadata is not null)
                {
                    _dbContext.Entry(metadata).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
                }

                _dbContext.Entry(sourceLookup.File).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
            },
            rollbackPhysicalState: () => RestoreMovedFileIfAvailable(backupPath, sourcePath),
            successAuditEntry: $"DELETE_FILE|{filePath}|SUCCESS",
            failureAuditPrefix: $"DELETE_FILE|{filePath}|FAIL",
            successMessage: "File deleted from storage and database.",
            rollbackFailureMessage: "Database update failed after physical delete. The operation was rolled back.",
            rollbackFailureErrorCode: OperationErrorCodes.DeleteFileUnexpected,
            cancellationToken: cancellationToken);

        if (!persistenceResult.Success)
        {
            return persistenceResult;
        }

        if (!TryDeleteFileIfExists(backupPath))
        {
            _auditTrailWriter.Write($"DELETE_FILE|{filePath}|CLEANUP_FAIL|{backupPath}");
            return new OperationResult(false, "File metadata was deleted, but physical cleanup did not complete.", OperationErrorCodes.DeleteFileCleanupFailed);
        }

        return persistenceResult;
    }

    public OperationResult DownloadFile(string filePath, string targetLocalPath)
    {
        var sourceLookup = StoragePathLookupQueries.FindFileByPath(_dbContext, filePath);
        if (sourceLookup.File is null)
        {
            return new OperationResult(false, $"File not found: {filePath}");
        }

        string fullTargetPath = Path.GetFullPath(targetLocalPath);
        string? targetDirectory = Path.GetDirectoryName(fullTargetPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        string sourcePhysicalPath = ResolvePhysicalPath(sourceLookup.File.RelativePath);

        try
        {
            File.Copy(sourcePhysicalPath, fullTargetPath, true);
            _auditTrailWriter.Write($"DOWNLOAD_FILE|{filePath}|{fullTargetPath}|SUCCESS");
            return new OperationResult(true, "File downloaded.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogDownloadFileFailedMessage(_logger, filePath, ex);
            _auditTrailWriter.Write($"DOWNLOAD_FILE|{filePath}|{fullTargetPath}|FAIL|{ex.Message}");
            return new OperationResult(false, ex.Message, OperationErrorCodes.UnexpectedError);
        }
    }

    public async Task<OperationResult> DownloadFileAsync(string filePath, string targetLocalPath, CancellationToken cancellationToken = default)
    {
        var sourceLookup = StoragePathLookupQueries.FindFileByPath(_dbContext, filePath);
        if (sourceLookup.File is null)
        {
            return new OperationResult(false, $"File not found: {filePath}");
        }

        string fullTargetPath = Path.GetFullPath(targetLocalPath);
        string? targetDirectory = Path.GetDirectoryName(fullTargetPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        string sourcePhysicalPath = ResolvePhysicalPath(sourceLookup.File.RelativePath);

        try
        {
            await using FileStream source = new(sourcePhysicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            await using FileStream target = new(fullTargetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await source.CopyToAsync(target, cancellationToken);
            _auditTrailWriter.Write($"DOWNLOAD_FILE|{filePath}|{fullTargetPath}|SUCCESS");
            return new OperationResult(true, "File downloaded.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogDownloadFileAsyncFailedMessage(_logger, filePath, ex);
            _auditTrailWriter.Write($"DOWNLOAD_FILE|{filePath}|{fullTargetPath}|FAIL|{ex.Message}");
            return new OperationResult(false, ex.Message, OperationErrorCodes.UnexpectedError);
        }
    }

    public FileDownloadResult DownloadFileContent(string filePath)
    {
        var sourceLookup = StoragePathLookupQueries.FindFileByPath(_dbContext, filePath);
        if (sourceLookup.File is null)
        {
            return new FileDownloadResult(false, $"File not found: {filePath}", string.Empty, null, "application/octet-stream");
        }

        string sourcePhysicalPath = ResolvePhysicalPath(sourceLookup.File.RelativePath);
        if (!File.Exists(sourcePhysicalPath))
        {
            return new FileDownloadResult(false, $"Physical file not found: {sourcePhysicalPath}", string.Empty, null, "application/octet-stream");
        }

        try
        {
            byte[] content = File.ReadAllBytes(sourcePhysicalPath);
            string contentType = FileContentTypeResolver.Resolve(sourceLookup.File.Name);
            _auditTrailWriter.Write($"DOWNLOAD_FILE_CONTENT|{filePath}|SUCCESS");
            return new FileDownloadResult(true, "File content loaded.", sourceLookup.File.Name, content, contentType);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogDownloadFileContentFailedMessage(_logger, filePath, ex);
            _auditTrailWriter.Write($"DOWNLOAD_FILE_CONTENT|{filePath}|FAIL|{ex.Message}");
            return new FileDownloadResult(false, ex.Message, string.Empty, null, "application/octet-stream");
        }
    }

    public async Task<FileDownloadResult> DownloadFileContentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var sourceLookup = StoragePathLookupQueries.FindFileByPath(_dbContext, filePath);
        if (sourceLookup.File is null)
        {
            return new FileDownloadResult(false, $"File not found: {filePath}", string.Empty, null, "application/octet-stream");
        }

        string sourcePhysicalPath = ResolvePhysicalPath(sourceLookup.File.RelativePath);
        if (!File.Exists(sourcePhysicalPath))
        {
            return new FileDownloadResult(false, $"Physical file not found: {sourcePhysicalPath}", string.Empty, null, "application/octet-stream");
        }

        try
        {
            byte[] content;
            await using (FileStream stream = new(sourcePhysicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true))
            {
                content = new byte[stream.Length];
                int bytesReadTotal = 0;
                while (bytesReadTotal < content.Length)
                {
                    int bytesRead = await stream.ReadAsync(content.AsMemory(bytesReadTotal, content.Length - bytesReadTotal), cancellationToken);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    bytesReadTotal += bytesRead;
                }

                if (bytesReadTotal != content.Length)
                {
                    Array.Resize(ref content, bytesReadTotal);
                }
            }

            string contentType = FileContentTypeResolver.Resolve(sourceLookup.File.Name);
            _auditTrailWriter.Write($"DOWNLOAD_FILE_CONTENT|{filePath}|SUCCESS");
            return new FileDownloadResult(true, "File content loaded.", sourceLookup.File.Name, content, contentType);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogDownloadFileContentAsyncFailedMessage(_logger, filePath, ex);
            _auditTrailWriter.Write($"DOWNLOAD_FILE_CONTENT|{filePath}|FAIL|{ex.Message}");
            return new FileDownloadResult(false, ex.Message, string.Empty, null, "application/octet-stream");
        }
    }

    private static bool TryRollbackFileMove(string sourcePath, string targetPath, bool caseOnlyRename = false)
    {
        try
        {
            if (!File.Exists(sourcePath))
            {
                return false;
            }

            string? targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            PhysicalPathMoveHelper.MoveFile(sourcePath, targetPath, caseOnlyRename);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryDeleteFileIfExists(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildPendingReplacePath(string sourcePath)
    {
        string? directory = Path.GetDirectoryName(sourcePath);
        string fileName = Path.GetFileName(sourcePath);
        string parent = string.IsNullOrWhiteSpace(directory) ? Path.GetTempPath() : directory;
        return Path.Combine(parent, $"{fileName}.pending-{Guid.NewGuid():N}");
    }

    private static bool TryMoveFileIfExists(string sourcePath, string destinationPath, out string? errorMessage)
    {
        errorMessage = null;
        if (!File.Exists(sourcePath))
        {
            return true;
        }

        try
        {
            string? targetDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Move(sourcePath, destinationPath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static bool RestoreMovedFileIfAvailable(string? sourcePath, string? targetPath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(targetPath))
        {
            return true;
        }

        try
        {
            if (!File.Exists(sourcePath))
            {
                return true;
            }

            if (File.Exists(targetPath))
            {
                return false;
            }

            string? parent = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.Move(sourcePath, targetPath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
