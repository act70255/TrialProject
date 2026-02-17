using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Models;
using CloudFileManager.Domain.Enums;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using CloudFileManager.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace CloudFileManager.Infrastructure.FileStorage;

public sealed partial class StorageMetadataGateway
{
    public OperationResult UploadFile(UploadFileRequest request, CloudFileType fileType)
    {
        DirectoryEntity? directory = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, request.DirectoryPath);
        if (directory is null)
        {
            return new OperationResult(false, $"Directory not found: {request.DirectoryPath}");
        }

        UploadConflictContext conflictContext = new();
        OperationResult? conflictResult = ResolveUploadConflict(directory, ref request, conflictContext);
        if (conflictResult is not null)
        {
            return conflictResult;
        }

        string extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        string directoryPhysicalPath = ResolvePhysicalPath(directory.RelativePath);
        string physicalPath = Path.Combine(directoryPhysicalPath, request.FileName);

        long persistedSize;
        try
        {
            persistedSize = PhysicalFileWriter.Write(physicalPath, request);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            RestoreMovedFileIfAvailable(conflictContext.BackupPath, conflictContext.OverwrittenFilePhysicalPath);
            RestoreUploadConflictState(conflictContext);
            _auditTrailWriter.Write($"UPLOAD|{request.DirectoryPath}/{request.FileName}|FAIL|{ex.Message}");
            return new OperationResult(false, ex.Message, OperationErrorCodes.UploadIoError);
        }

        FileEntity fileEntity = new()
        {
            Id = Guid.NewGuid(),
            DirectoryId = directory.Id,
            Name = request.FileName,
            Extension = extension,
            SizeBytes = persistedSize,
            CreatedTime = DateTime.UtcNow,
            FileType = (int)fileType,
            CreationOrder = _dbContext.Files.Count(item => item.DirectoryId == directory.Id) + 1,
            RelativePath = ToStoredPath(physicalPath)
        };

        FileMetadataEntity metadata = StorageFileMetadataMapper.CreateMetadata(fileEntity.Id, fileType, request);

        _dbContext.Files.Add(fileEntity);
        _dbContext.FileMetadata.Add(metadata);

        OperationResult persistenceResult = ExecuteSaveWithRollback(
            restoreEntityState: () =>
            {
                _dbContext.ChangeTracker.Clear();
                RestoreUploadConflictState(conflictContext);
            },
            rollbackPhysicalState: () => RollbackUploadPhysicalState(physicalPath, conflictContext),
            successAuditEntry: $"UPLOAD|{request.DirectoryPath}/{request.FileName}|SUCCESS",
            failureAuditPrefix: $"UPLOAD|{request.DirectoryPath}/{request.FileName}|FAIL",
            successMessage: "File persisted.",
            rollbackFailureMessage: "File upload failed while saving metadata.",
            rollbackFailureErrorCode: OperationErrorCodes.UploadMetadataSaveFailed);

        if (!persistenceResult.Success)
        {
            return persistenceResult;
        }

        InvalidateRootTreeCache();
        CleanupUploadConflictBackup(conflictContext, request);
        return persistenceResult;
    }

    public async Task<OperationResult> UploadFileAsync(UploadFileRequest request, CloudFileType fileType, CancellationToken cancellationToken = default)
    {
        DirectoryEntity? directory = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, request.DirectoryPath);
        if (directory is null)
        {
            return new OperationResult(false, $"Directory not found: {request.DirectoryPath}");
        }

        UploadConflictContext conflictContext = new();
        OperationResult? conflictResult = ResolveUploadConflict(directory, ref request, conflictContext);
        if (conflictResult is not null)
        {
            return conflictResult;
        }

        string extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        string directoryPhysicalPath = ResolvePhysicalPath(directory.RelativePath);
        string physicalPath = Path.Combine(directoryPhysicalPath, request.FileName);

        long persistedSize;
        try
        {
            persistedSize = await PhysicalFileWriter.WriteAsync(physicalPath, request, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            RestoreMovedFileIfAvailable(conflictContext.BackupPath, conflictContext.OverwrittenFilePhysicalPath);
            RestoreUploadConflictState(conflictContext);
            _auditTrailWriter.Write($"UPLOAD|{request.DirectoryPath}/{request.FileName}|FAIL|{ex.Message}");
            return new OperationResult(false, ex.Message, OperationErrorCodes.UploadIoError);
        }

        FileEntity fileEntity = new()
        {
            Id = Guid.NewGuid(),
            DirectoryId = directory.Id,
            Name = request.FileName,
            Extension = extension,
            SizeBytes = persistedSize,
            CreatedTime = DateTime.UtcNow,
            FileType = (int)fileType,
            CreationOrder = _dbContext.Files.Count(item => item.DirectoryId == directory.Id) + 1,
            RelativePath = ToStoredPath(physicalPath)
        };

        FileMetadataEntity metadata = StorageFileMetadataMapper.CreateMetadata(fileEntity.Id, fileType, request);

        _dbContext.Files.Add(fileEntity);
        _dbContext.FileMetadata.Add(metadata);

        OperationResult persistenceResult = await ExecuteSaveWithRollbackAsync(
            restoreEntityState: () =>
            {
                _dbContext.ChangeTracker.Clear();
                RestoreUploadConflictState(conflictContext);
            },
            rollbackPhysicalState: () => RollbackUploadPhysicalState(physicalPath, conflictContext),
            successAuditEntry: $"UPLOAD|{request.DirectoryPath}/{request.FileName}|SUCCESS",
            failureAuditPrefix: $"UPLOAD|{request.DirectoryPath}/{request.FileName}|FAIL",
            successMessage: "File persisted.",
            rollbackFailureMessage: "File upload failed while saving metadata.",
            rollbackFailureErrorCode: OperationErrorCodes.UploadMetadataSaveFailed,
            cancellationToken: cancellationToken);

        if (!persistenceResult.Success)
        {
            return persistenceResult;
        }

        InvalidateRootTreeCache();
        CleanupUploadConflictBackup(conflictContext, request);
        return persistenceResult;
    }

    private OperationResult? ResolveUploadConflict(DirectoryEntity directory, ref UploadFileRequest request, UploadConflictContext conflictContext)
    {
        bool duplicated = StorageNameConflictQueries.HasFileNameConflict(_dbContext, directory.Id, request.FileName, excludeFileId: null);
        if (!duplicated)
        {
            return null;
        }

        switch (_management.GetFileConflictPolicy())
        {
            case FileConflictPolicyType.Reject:
                return new OperationResult(false, $"File already exists: {request.FileName}");
            case FileConflictPolicyType.Overwrite:
                FileEntity? existing = StorageNameConflictQueries.FindFileByName(_dbContext, directory.Id, request.FileName, excludeFileId: null);
                if (existing is not null)
                {
                    string existingPhysicalPath = ResolvePhysicalPath(existing.RelativePath);
                    string backupPath = BuildPendingReplacePath(existingPhysicalPath);
                    if (!TryMoveFileIfExists(existingPhysicalPath, backupPath, out string? errorMessage))
                    {
                        _auditTrailWriter.Write($"UPLOAD|{request.DirectoryPath}/{request.FileName}|FAIL|{errorMessage}");
                        return new OperationResult(false, errorMessage ?? "Unable to prepare overwrite target.");
                    }

                    conflictContext.OverwrittenFile = existing;
                    conflictContext.OverwrittenFilePhysicalPath = existingPhysicalPath;
                    conflictContext.BackupPath = backupPath;
                    conflictContext.OverwrittenMetadata = DeleteExistingFileEntity(existing);
                }

                return null;
            case FileConflictPolicyType.Rename:
                request = new UploadFileRequest(
                    request.DirectoryPath,
                    UniqueFileNameResolver.Resolve(
                        request.FileName,
                        candidate => _dbContext.Files.Any(item => item.DirectoryId == directory.Id && item.Name == candidate)),
                    request.Size,
                    request.PageCount,
                    request.Width,
                    request.Height,
                    request.Encoding,
                    request.SourceLocalPath);
                return null;
            default:
                return new OperationResult(false, $"Unsupported file conflict policy: {_management.FileConflictPolicy}");
        }
    }

    private static bool RollbackUploadPhysicalState(string uploadedPath, UploadConflictContext conflictContext)
    {
        bool deletedUploaded = TryDeleteFileIfExists(uploadedPath);
        bool restoredOverwritten = RestoreMovedFileIfAvailable(conflictContext.BackupPath, conflictContext.OverwrittenFilePhysicalPath);
        return deletedUploaded && restoredOverwritten;
    }

    private void RestoreUploadConflictState(UploadConflictContext conflictContext)
    {
        if (conflictContext.OverwrittenMetadata is not null)
        {
            _dbContext.Entry(conflictContext.OverwrittenMetadata).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
        }

        if (conflictContext.OverwrittenFile is not null)
        {
            _dbContext.Entry(conflictContext.OverwrittenFile).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
        }
    }

    private void CleanupUploadConflictBackup(UploadConflictContext conflictContext, UploadFileRequest request)
    {
        if (string.IsNullOrWhiteSpace(conflictContext.BackupPath))
        {
            return;
        }

        if (!TryDeleteFileIfExists(conflictContext.BackupPath))
        {
            _auditTrailWriter.Write($"UPLOAD|{request.DirectoryPath}/{request.FileName}|CLEANUP_FAIL|{conflictContext.BackupPath}");
        }
    }

    private FileMetadataEntity? DeleteExistingFileEntity(FileEntity fileEntity)
    {
        FileMetadataEntity? metadata = _dbContext.FileMetadata.FirstOrDefault(item => item.FileId == fileEntity.Id);
        if (metadata is not null)
        {
            _dbContext.FileMetadata.Remove(metadata);
        }

        _dbContext.Files.Remove(fileEntity);
        return metadata;
    }

    private sealed class UploadConflictContext
    {
        public FileEntity? OverwrittenFile { get; set; }

        public FileMetadataEntity? OverwrittenMetadata { get; set; }

        public string? BackupPath { get; set; }

        public string? OverwrittenFilePhysicalPath { get; set; }
    }
}
