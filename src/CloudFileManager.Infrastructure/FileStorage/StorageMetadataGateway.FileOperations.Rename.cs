using CloudFileManager.Application.Models;
using CloudFileManager.Shared.Common;

namespace CloudFileManager.Infrastructure.FileStorage;

public sealed partial class StorageMetadataGateway
{
    public OperationResult RenameFile(string filePath, string newFileName)
    {
        var sourceLookup = StoragePathLookupQueries.FindFileByPath(_dbContext, filePath);
        if (sourceLookup.File is null || sourceLookup.Directory is null)
        {
            return new OperationResult(false, $"File not found: {filePath}");
        }

        bool duplicated = StorageNameConflictQueries.HasFileNameConflict(_dbContext, sourceLookup.Directory.Id, newFileName, sourceLookup.File.Id);
        if (duplicated)
        {
            return new OperationResult(false, $"File name already exists: {newFileName}");
        }

        string sourcePhysicalPath = ResolvePhysicalPath(sourceLookup.File.RelativePath);
        string directoryPhysicalPath = ResolvePhysicalPath(sourceLookup.Directory.RelativePath);
        string destinationPath = Path.Combine(directoryPhysicalPath, newFileName);
        if (!File.Exists(sourcePhysicalPath))
        {
            return new OperationResult(false, $"Physical source file not found: {sourcePhysicalPath}");
        }

        if (!Directory.Exists(directoryPhysicalPath))
        {
            return new OperationResult(false, $"Physical target directory not found: {directoryPhysicalPath}");
        }

        try
        {
            bool caseOnlyRename = string.Equals(sourceLookup.File.Name, newFileName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(sourceLookup.File.Name, newFileName, StringComparison.Ordinal);
            PhysicalPathMoveHelper.MoveFile(sourcePhysicalPath, destinationPath, caseOnlyRename);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _auditTrailWriter.Write($"RENAME_FILE|{filePath}|{newFileName}|FAIL|{ex.Message}");
            return new OperationResult(false, ex.Message);
        }

        string originalName = sourceLookup.File.Name;
        string originalExtension = sourceLookup.File.Extension;
        string originalPhysicalPath = sourceLookup.File.RelativePath;

        sourceLookup.File.Name = newFileName;
        sourceLookup.File.Extension = Path.GetExtension(newFileName).ToLowerInvariant();
        sourceLookup.File.RelativePath = ToStoredPath(destinationPath);

        bool rollbackCaseOnlyRename = string.Equals(newFileName, originalName, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(newFileName, originalName, StringComparison.Ordinal);

        OperationResult persistenceResult = ExecuteSaveWithRollback(
            restoreEntityState: () =>
            {
                sourceLookup.File.Name = originalName;
                sourceLookup.File.Extension = originalExtension;
                sourceLookup.File.RelativePath = originalPhysicalPath;
            },
            rollbackPhysicalState: () => TryRollbackFileMove(destinationPath, ResolvePhysicalPath(originalPhysicalPath), rollbackCaseOnlyRename),
            successAuditEntry: $"RENAME_FILE|{filePath}|{newFileName}|SUCCESS",
            failureAuditPrefix: $"RENAME_FILE|{filePath}|{newFileName}|FAIL",
            successMessage: "File renamed and persisted.",
            rollbackFailureMessage: "Database update failed after physical rename. The operation was rolled back.",
            rollbackFailureErrorCode: OperationErrorCodes.RenameFileUnexpected);

        if (persistenceResult.Success)
        {
            InvalidateRootTreeCache();
        }

        return persistenceResult;
    }

    public Task<OperationResult> RenameFileAsync(string filePath, string newFileName, CancellationToken cancellationToken = default)
    {
        return RenameFileCoreAsync(filePath, newFileName, cancellationToken);
    }

    private async Task<OperationResult> RenameFileCoreAsync(string filePath, string newFileName, CancellationToken cancellationToken)
    {
        var sourceLookup = StoragePathLookupQueries.FindFileByPath(_dbContext, filePath);
        if (sourceLookup.File is null || sourceLookup.Directory is null)
        {
            return new OperationResult(false, $"File not found: {filePath}");
        }

        bool duplicated = StorageNameConflictQueries.HasFileNameConflict(_dbContext, sourceLookup.Directory.Id, newFileName, sourceLookup.File.Id);
        if (duplicated)
        {
            return new OperationResult(false, $"File name already exists: {newFileName}");
        }

        string sourcePhysicalPath = ResolvePhysicalPath(sourceLookup.File.RelativePath);
        string directoryPhysicalPath = ResolvePhysicalPath(sourceLookup.Directory.RelativePath);
        string destinationPath = Path.Combine(directoryPhysicalPath, newFileName);
        if (!File.Exists(sourcePhysicalPath))
        {
            return new OperationResult(false, $"Physical source file not found: {sourcePhysicalPath}");
        }

        if (!Directory.Exists(directoryPhysicalPath))
        {
            return new OperationResult(false, $"Physical target directory not found: {directoryPhysicalPath}");
        }

        try
        {
            bool caseOnlyRename = string.Equals(sourceLookup.File.Name, newFileName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(sourceLookup.File.Name, newFileName, StringComparison.Ordinal);
            PhysicalPathMoveHelper.MoveFile(sourcePhysicalPath, destinationPath, caseOnlyRename);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _auditTrailWriter.Write($"RENAME_FILE|{filePath}|{newFileName}|FAIL|{ex.Message}");
            return new OperationResult(false, ex.Message);
        }

        string originalName = sourceLookup.File.Name;
        string originalExtension = sourceLookup.File.Extension;
        string originalPhysicalPath = sourceLookup.File.RelativePath;

        sourceLookup.File.Name = newFileName;
        sourceLookup.File.Extension = Path.GetExtension(newFileName).ToLowerInvariant();
        sourceLookup.File.RelativePath = ToStoredPath(destinationPath);

        bool rollbackCaseOnlyRename = string.Equals(newFileName, originalName, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(newFileName, originalName, StringComparison.Ordinal);

        OperationResult persistenceResult = await ExecuteSaveWithRollbackAsync(
            restoreEntityState: () =>
            {
                sourceLookup.File.Name = originalName;
                sourceLookup.File.Extension = originalExtension;
                sourceLookup.File.RelativePath = originalPhysicalPath;
            },
            rollbackPhysicalState: () => TryRollbackFileMove(destinationPath, ResolvePhysicalPath(originalPhysicalPath), rollbackCaseOnlyRename),
            successAuditEntry: $"RENAME_FILE|{filePath}|{newFileName}|SUCCESS",
            failureAuditPrefix: $"RENAME_FILE|{filePath}|{newFileName}|FAIL",
            successMessage: "File renamed and persisted.",
            rollbackFailureMessage: "Database update failed after physical rename. The operation was rolled back.",
            rollbackFailureErrorCode: OperationErrorCodes.RenameFileUnexpected,
            cancellationToken: cancellationToken);

        if (persistenceResult.Success)
        {
            InvalidateRootTreeCache();
        }

        return persistenceResult;
    }
}
