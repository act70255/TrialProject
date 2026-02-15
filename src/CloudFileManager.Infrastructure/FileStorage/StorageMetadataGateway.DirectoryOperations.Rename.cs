using CloudFileManager.Application.Models;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using CloudFileManager.Shared.Common;

namespace CloudFileManager.Infrastructure.FileStorage;

public sealed partial class StorageMetadataGateway
{
    public OperationResult RenameDirectory(string directoryPath, string newDirectoryName)
    {
        if (IsRootDirectoryPath(directoryPath))
        {
            return new OperationResult(false, "Root directory cannot be renamed.");
        }

        DirectoryEntity? directory = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, directoryPath);
        if (directory is null)
        {
            return new OperationResult(false, $"Directory not found: {directoryPath}");
        }

        if (StorageNameConflictQueries.HasDirectoryNameConflict(_dbContext, directory.ParentId, newDirectoryName, directory.Id))
        {
            return new OperationResult(false, $"Directory already exists: {newDirectoryName}");
        }

        string directoryPhysicalPath = ResolvePhysicalPath(directory.RelativePath);
        string parentPath = Directory.GetParent(directoryPhysicalPath)?.FullName ?? _storageRootPath;
        string destinationPath = Path.Combine(parentPath, newDirectoryName);
        OperationResult? sourcePathValidation = ValidatePhysicalDirectory(directoryPhysicalPath, "source");
        if (sourcePathValidation is not null)
        {
            return sourcePathValidation;
        }

        OperationResult? parentPathValidation = ValidatePhysicalDirectory(parentPath, "parent");
        if (parentPathValidation is not null)
        {
            return parentPathValidation;
        }

        try
        {
            bool caseOnlyRename = string.Equals(directory.Name, newDirectoryName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(directory.Name, newDirectoryName, StringComparison.Ordinal);
            PhysicalPathMoveHelper.MoveDirectory(directoryPhysicalPath, destinationPath, caseOnlyRename);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _auditTrailWriter.Write($"RENAME_DIRECTORY|{directoryPath}|{newDirectoryName}|FAIL|{ex.Message}");
            return new OperationResult(false, ex.Message);
        }

        string originalName = directory.Name;
        string originalPhysicalPath = directory.RelativePath;

        directory.Name = newDirectoryName;
        directory.RelativePath = ToStoredPath(destinationPath);
        StorageDirectoryMutations.UpdateDescendantPhysicalPaths(_dbContext, directory.Id, directory.RelativePath);

        bool rollbackCaseOnlyRename = string.Equals(newDirectoryName, originalName, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(newDirectoryName, originalName, StringComparison.Ordinal);

        return ExecuteSaveWithRollback(
            restoreEntityState: () =>
            {
                directory.Name = originalName;
                directory.RelativePath = originalPhysicalPath;
                StorageDirectoryMutations.UpdateDescendantPhysicalPaths(_dbContext, directory.Id, directory.RelativePath);
            },
            rollbackPhysicalState: () => TryRollbackDirectoryMove(destinationPath, ResolvePhysicalPath(originalPhysicalPath), rollbackCaseOnlyRename),
            successAuditEntry: $"RENAME_DIRECTORY|{directoryPath}|{newDirectoryName}|SUCCESS",
            failureAuditPrefix: $"RENAME_DIRECTORY|{directoryPath}|{newDirectoryName}|FAIL",
            successMessage: "Directory renamed.",
            rollbackFailureMessage: "Database update failed after physical rename. The operation was rolled back.",
            rollbackFailureErrorCode: OperationErrorCodes.RenameDirectoryUnexpected);
    }

    public Task<OperationResult> RenameDirectoryAsync(string directoryPath, string newDirectoryName, CancellationToken cancellationToken = default)
    {
        return RenameDirectoryCoreAsync(directoryPath, newDirectoryName, cancellationToken);
    }

    private async Task<OperationResult> RenameDirectoryCoreAsync(string directoryPath, string newDirectoryName, CancellationToken cancellationToken)
    {
        if (IsRootDirectoryPath(directoryPath))
        {
            return new OperationResult(false, "Root directory cannot be renamed.");
        }

        DirectoryEntity? directory = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, directoryPath);
        if (directory is null)
        {
            return new OperationResult(false, $"Directory not found: {directoryPath}");
        }

        if (StorageNameConflictQueries.HasDirectoryNameConflict(_dbContext, directory.ParentId, newDirectoryName, directory.Id))
        {
            return new OperationResult(false, $"Directory already exists: {newDirectoryName}");
        }

        string directoryPhysicalPath = ResolvePhysicalPath(directory.RelativePath);
        string parentPath = Directory.GetParent(directoryPhysicalPath)?.FullName ?? _storageRootPath;
        string destinationPath = Path.Combine(parentPath, newDirectoryName);
        OperationResult? sourcePathValidation = ValidatePhysicalDirectory(directoryPhysicalPath, "source");
        if (sourcePathValidation is not null)
        {
            return sourcePathValidation;
        }

        OperationResult? parentPathValidation = ValidatePhysicalDirectory(parentPath, "parent");
        if (parentPathValidation is not null)
        {
            return parentPathValidation;
        }

        try
        {
            bool caseOnlyRename = string.Equals(directory.Name, newDirectoryName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(directory.Name, newDirectoryName, StringComparison.Ordinal);
            PhysicalPathMoveHelper.MoveDirectory(directoryPhysicalPath, destinationPath, caseOnlyRename);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _auditTrailWriter.Write($"RENAME_DIRECTORY|{directoryPath}|{newDirectoryName}|FAIL|{ex.Message}");
            return new OperationResult(false, ex.Message);
        }

        string originalName = directory.Name;
        string originalPhysicalPath = directory.RelativePath;

        directory.Name = newDirectoryName;
        directory.RelativePath = ToStoredPath(destinationPath);
        StorageDirectoryMutations.UpdateDescendantPhysicalPaths(_dbContext, directory.Id, directory.RelativePath);

        bool rollbackCaseOnlyRename = string.Equals(newDirectoryName, originalName, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(newDirectoryName, originalName, StringComparison.Ordinal);

        return await ExecuteSaveWithRollbackAsync(
            restoreEntityState: () =>
            {
                directory.Name = originalName;
                directory.RelativePath = originalPhysicalPath;
                StorageDirectoryMutations.UpdateDescendantPhysicalPaths(_dbContext, directory.Id, directory.RelativePath);
            },
            rollbackPhysicalState: () => TryRollbackDirectoryMove(destinationPath, ResolvePhysicalPath(originalPhysicalPath), rollbackCaseOnlyRename),
            successAuditEntry: $"RENAME_DIRECTORY|{directoryPath}|{newDirectoryName}|SUCCESS",
            failureAuditPrefix: $"RENAME_DIRECTORY|{directoryPath}|{newDirectoryName}|FAIL",
            successMessage: "Directory renamed.",
            rollbackFailureMessage: "Database update failed after physical rename. The operation was rolled back.",
            rollbackFailureErrorCode: OperationErrorCodes.RenameDirectoryUnexpected,
            cancellationToken: cancellationToken);
    }

    private static OperationResult? ValidatePhysicalDirectory(string path, string role)
    {
        if (Directory.Exists(path))
        {
            return null;
        }

        return new OperationResult(false, $"Physical {role} directory not found: {path}");
    }

    private static bool TryRollbackDirectoryMove(string sourcePath, string targetPath, bool caseOnlyRename = false)
    {
        try
        {
            if (!Directory.Exists(sourcePath))
            {
                return false;
            }

            string? targetDirectory = Directory.GetParent(targetPath)?.FullName;
            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            PhysicalPathMoveHelper.MoveDirectory(sourcePath, targetPath, caseOnlyRename);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
