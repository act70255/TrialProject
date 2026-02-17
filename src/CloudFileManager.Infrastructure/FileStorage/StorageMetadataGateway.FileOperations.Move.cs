using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Models;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using CloudFileManager.Shared.Common;

namespace CloudFileManager.Infrastructure.FileStorage;

public sealed partial class StorageMetadataGateway
{
    public OperationResult MoveFile(string sourceFilePath, string targetDirectoryPath)
    {
        var sourceLookup = StoragePathLookupQueries.FindFileByPath(_dbContext, sourceFilePath);
        if (sourceLookup.File is null)
        {
            return new OperationResult(false, $"File not found: {sourceFilePath}");
        }

        DirectoryEntity? targetDirectory = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, targetDirectoryPath);
        if (targetDirectory is null)
        {
            return new OperationResult(false, $"Target directory not found: {targetDirectoryPath}");
        }

        MoveConflictContext conflictContext = new();
        OperationResult? conflictResult = ResolveMoveConflict(targetDirectory, sourceLookup.File, conflictContext);
        if (conflictResult is not null)
        {
            return conflictResult;
        }

        string sourcePath = ResolvePhysicalPath(sourceLookup.File.RelativePath);
        string targetDirectoryPhysicalPath = ResolvePhysicalPath(targetDirectory.RelativePath);
        string destinationPath = Path.Combine(targetDirectoryPhysicalPath, sourceLookup.File.Name);
        if (!File.Exists(sourcePath))
        {
            return new OperationResult(false, $"Physical source file not found: {sourcePath}");
        }

        if (!Directory.Exists(targetDirectoryPhysicalPath))
        {
            return new OperationResult(false, $"Physical target directory not found: {targetDirectoryPhysicalPath}");
        }

        try
        {
            File.Move(sourcePath, destinationPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _auditTrailWriter.Write($"MOVE_FILE|{sourceFilePath}|{targetDirectoryPath}|FAIL|{ex.Message}");
            return new OperationResult(false, ex.Message);
        }

        Guid originalDirectoryId = sourceLookup.File.DirectoryId;
        string originalPhysicalPath = sourceLookup.File.RelativePath;
        int originalCreationOrder = sourceLookup.File.CreationOrder;

        sourceLookup.File.DirectoryId = targetDirectory.Id;
        sourceLookup.File.RelativePath = ToStoredPath(destinationPath);
        sourceLookup.File.CreationOrder = _dbContext.Files.Count(item => item.DirectoryId == targetDirectory.Id) + 1;

        OperationResult persistenceResult = ExecuteSaveWithRollback(
            restoreEntityState: () =>
            {
                sourceLookup.File.DirectoryId = originalDirectoryId;
                sourceLookup.File.RelativePath = originalPhysicalPath;
                sourceLookup.File.CreationOrder = originalCreationOrder;
                RestoreMoveConflictState(conflictContext);
            },
            rollbackPhysicalState: () => RollbackMovePhysicalState(destinationPath, sourcePath, conflictContext),
            successAuditEntry: $"MOVE_FILE|{sourceFilePath}|{targetDirectoryPath}|SUCCESS",
            failureAuditPrefix: $"MOVE_FILE|{sourceFilePath}|{targetDirectoryPath}|FAIL",
            successMessage: "File moved and persisted.",
            rollbackFailureMessage: "Database update failed after physical move. The operation was rolled back.",
            rollbackFailureErrorCode: OperationErrorCodes.MoveFileUnexpected);

        if (!persistenceResult.Success)
        {
            return persistenceResult;
        }

        InvalidateRootTreeCache();
        CleanupMoveConflictBackup(conflictContext, sourceFilePath, targetDirectoryPath);
        return persistenceResult;
    }

    public Task<OperationResult> MoveFileAsync(string sourceFilePath, string targetDirectoryPath, CancellationToken cancellationToken = default)
    {
        return MoveFileCoreAsync(sourceFilePath, targetDirectoryPath, cancellationToken);
    }

    private async Task<OperationResult> MoveFileCoreAsync(string sourceFilePath, string targetDirectoryPath, CancellationToken cancellationToken)
    {
        var sourceLookup = StoragePathLookupQueries.FindFileByPath(_dbContext, sourceFilePath);
        if (sourceLookup.File is null)
        {
            return new OperationResult(false, $"File not found: {sourceFilePath}");
        }

        DirectoryEntity? targetDirectory = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, targetDirectoryPath);
        if (targetDirectory is null)
        {
            return new OperationResult(false, $"Target directory not found: {targetDirectoryPath}");
        }

        MoveConflictContext conflictContext = new();
        OperationResult? conflictResult = ResolveMoveConflict(targetDirectory, sourceLookup.File, conflictContext);
        if (conflictResult is not null)
        {
            return conflictResult;
        }

        string sourcePath = ResolvePhysicalPath(sourceLookup.File.RelativePath);
        string targetDirectoryPhysicalPath = ResolvePhysicalPath(targetDirectory.RelativePath);
        string destinationPath = Path.Combine(targetDirectoryPhysicalPath, sourceLookup.File.Name);
        if (!File.Exists(sourcePath))
        {
            return new OperationResult(false, $"Physical source file not found: {sourcePath}");
        }

        if (!Directory.Exists(targetDirectoryPhysicalPath))
        {
            return new OperationResult(false, $"Physical target directory not found: {targetDirectoryPhysicalPath}");
        }

        try
        {
            File.Move(sourcePath, destinationPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _auditTrailWriter.Write($"MOVE_FILE|{sourceFilePath}|{targetDirectoryPath}|FAIL|{ex.Message}");
            return new OperationResult(false, ex.Message);
        }

        Guid originalDirectoryId = sourceLookup.File.DirectoryId;
        string originalPhysicalPath = sourceLookup.File.RelativePath;
        int originalCreationOrder = sourceLookup.File.CreationOrder;

        sourceLookup.File.DirectoryId = targetDirectory.Id;
        sourceLookup.File.RelativePath = ToStoredPath(destinationPath);
        sourceLookup.File.CreationOrder = _dbContext.Files.Count(item => item.DirectoryId == targetDirectory.Id) + 1;

        OperationResult persistenceResult = await ExecuteSaveWithRollbackAsync(
            restoreEntityState: () =>
            {
                sourceLookup.File.DirectoryId = originalDirectoryId;
                sourceLookup.File.RelativePath = originalPhysicalPath;
                sourceLookup.File.CreationOrder = originalCreationOrder;
                RestoreMoveConflictState(conflictContext);
            },
            rollbackPhysicalState: () => RollbackMovePhysicalState(destinationPath, sourcePath, conflictContext),
            successAuditEntry: $"MOVE_FILE|{sourceFilePath}|{targetDirectoryPath}|SUCCESS",
            failureAuditPrefix: $"MOVE_FILE|{sourceFilePath}|{targetDirectoryPath}|FAIL",
            successMessage: "File moved and persisted.",
            rollbackFailureMessage: "Database update failed after physical move. The operation was rolled back.",
            rollbackFailureErrorCode: OperationErrorCodes.MoveFileUnexpected,
            cancellationToken: cancellationToken);

        if (!persistenceResult.Success)
        {
            return persistenceResult;
        }

        InvalidateRootTreeCache();
        CleanupMoveConflictBackup(conflictContext, sourceFilePath, targetDirectoryPath);
        return persistenceResult;
    }

    private OperationResult? ResolveMoveConflict(DirectoryEntity targetDirectory, FileEntity sourceFile, MoveConflictContext conflictContext)
    {
        bool duplicated = StorageNameConflictQueries.HasFileNameConflict(_dbContext, targetDirectory.Id, sourceFile.Name, excludeFileId: null);
        if (!duplicated)
        {
            return null;
        }

        switch (_management.GetFileConflictPolicy())
        {
            case FileConflictPolicyType.Reject:
                return new OperationResult(false, $"File already exists in target directory: {sourceFile.Name}");
            case FileConflictPolicyType.Overwrite:
                FileEntity? existing = StorageNameConflictQueries.FindFileByName(_dbContext, targetDirectory.Id, sourceFile.Name, excludeFileId: null);
                if (existing is not null)
                {
                    string existingPhysicalPath = ResolvePhysicalPath(existing.RelativePath);
                    string backupPath = BuildPendingReplacePath(existingPhysicalPath);
                    if (!TryMoveFileIfExists(existingPhysicalPath, backupPath, out string? errorMessage))
                    {
                        _auditTrailWriter.Write($"MOVE_FILE|{sourceFile.Name}|{ResolvePhysicalPath(targetDirectory.RelativePath)}|FAIL|{errorMessage}");
                        return new OperationResult(false, errorMessage ?? "Unable to prepare overwrite target.");
                    }

                    conflictContext.OverwrittenFile = existing;
                    conflictContext.OverwrittenFilePhysicalPath = existingPhysicalPath;
                    conflictContext.BackupPath = backupPath;
                    conflictContext.OverwrittenMetadata = DeleteExistingFileEntity(existing);
                }

                return null;
            case FileConflictPolicyType.Rename:
                sourceFile.Name = UniqueFileNameResolver.Resolve(
                    sourceFile.Name,
                    candidate => _dbContext.Files.Any(item => item.DirectoryId == targetDirectory.Id && item.Name == candidate));
                return null;
            default:
                return new OperationResult(false, $"Unsupported file conflict policy: {_management.FileConflictPolicy}");
        }
    }

    private static bool RollbackMovePhysicalState(string destinationPath, string sourcePath, MoveConflictContext conflictContext)
    {
        bool rollbackMovedFile = TryRollbackFileMove(destinationPath, sourcePath);
        bool rollbackOverwrittenFile = RestoreMovedFileIfAvailable(conflictContext.BackupPath, conflictContext.OverwrittenFilePhysicalPath);
        return rollbackMovedFile && rollbackOverwrittenFile;
    }

    private void RestoreMoveConflictState(MoveConflictContext conflictContext)
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

    private void CleanupMoveConflictBackup(MoveConflictContext conflictContext, string sourceFilePath, string targetDirectoryPath)
    {
        if (string.IsNullOrWhiteSpace(conflictContext.BackupPath))
        {
            return;
        }

        if (!TryDeleteFileIfExists(conflictContext.BackupPath))
        {
            _auditTrailWriter.Write($"MOVE_FILE|{sourceFilePath}|{targetDirectoryPath}|CLEANUP_FAIL|{conflictContext.BackupPath}");
        }
    }

    private sealed class MoveConflictContext
    {
        public FileEntity? OverwrittenFile { get; set; }

        public FileMetadataEntity? OverwrittenMetadata { get; set; }

        public string? BackupPath { get; set; }

        public string? OverwrittenFilePhysicalPath { get; set; }
    }
}
