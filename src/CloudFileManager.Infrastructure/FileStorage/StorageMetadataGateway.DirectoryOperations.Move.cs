using CloudFileManager.Application.Models;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using CloudFileManager.Shared.Common;

namespace CloudFileManager.Infrastructure.FileStorage;

public sealed partial class StorageMetadataGateway
{
    public OperationResult MoveDirectory(string sourceDirectoryPath, string targetParentDirectoryPath)
    {
        if (IsRootDirectoryPath(sourceDirectoryPath))
        {
            return new OperationResult(false, "Root directory cannot be moved.");
        }

        if (PathHierarchyRules.IsSameOrDescendant(sourceDirectoryPath, targetParentDirectoryPath))
        {
            return new OperationResult(false, "Cannot move a directory into itself or its descendants.");
        }

        DirectoryEntity? source = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, sourceDirectoryPath);
        DirectoryEntity? targetParent = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, targetParentDirectoryPath);
        if (source is null || targetParent is null)
        {
            return new OperationResult(false, "Source or target directory not found.");
        }

        if (StorageNameConflictQueries.HasDirectoryNameConflict(_dbContext, targetParent.Id, source.Name, excludeDirectoryId: null))
        {
            return new OperationResult(false, $"Directory already exists: {source.Name}");
        }

        string sourcePhysicalPath = ResolvePhysicalPath(source.RelativePath);
        string targetParentPhysicalPath = ResolvePhysicalPath(targetParent.RelativePath);
        string destinationPath = Path.Combine(targetParentPhysicalPath, source.Name);
        OperationResult? sourcePathValidation = ValidatePhysicalDirectory(sourcePhysicalPath, "source");
        if (sourcePathValidation is not null)
        {
            return sourcePathValidation;
        }

        OperationResult? targetPathValidation = ValidatePhysicalDirectory(targetParentPhysicalPath, "target");
        if (targetPathValidation is not null)
        {
            return targetPathValidation;
        }

        try
        {
            Directory.Move(sourcePhysicalPath, destinationPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _auditTrailWriter.Write($"MOVE_DIRECTORY|{sourceDirectoryPath}|{targetParentDirectoryPath}|FAIL|{ex.Message}");
            return new OperationResult(false, ex.Message);
        }

        Guid? originalParentId = source.ParentId;
        string originalPhysicalPath = source.RelativePath;

        source.ParentId = targetParent.Id;
        source.RelativePath = ToStoredPath(destinationPath);
        StorageDirectoryMutations.UpdateDescendantPhysicalPaths(_dbContext, source.Id, source.RelativePath);

        return ExecuteSaveWithRollback(
            restoreEntityState: () =>
            {
                source.ParentId = originalParentId;
                source.RelativePath = originalPhysicalPath;
                StorageDirectoryMutations.UpdateDescendantPhysicalPaths(_dbContext, source.Id, source.RelativePath);
            },
            rollbackPhysicalState: () => TryRollbackDirectoryMove(destinationPath, sourcePhysicalPath),
            successAuditEntry: $"MOVE_DIRECTORY|{sourceDirectoryPath}|{targetParentDirectoryPath}|SUCCESS",
            failureAuditPrefix: $"MOVE_DIRECTORY|{sourceDirectoryPath}|{targetParentDirectoryPath}|FAIL",
            successMessage: "Directory moved.",
            rollbackFailureMessage: "Database update failed after physical move. The operation was rolled back.",
            rollbackFailureErrorCode: OperationErrorCodes.MoveDirectoryUnexpected);
    }

    public Task<OperationResult> MoveDirectoryAsync(string sourceDirectoryPath, string targetParentDirectoryPath, CancellationToken cancellationToken = default)
    {
        return MoveDirectoryCoreAsync(sourceDirectoryPath, targetParentDirectoryPath, cancellationToken);
    }

    private async Task<OperationResult> MoveDirectoryCoreAsync(string sourceDirectoryPath, string targetParentDirectoryPath, CancellationToken cancellationToken)
    {
        if (IsRootDirectoryPath(sourceDirectoryPath))
        {
            return new OperationResult(false, "Root directory cannot be moved.");
        }

        if (PathHierarchyRules.IsSameOrDescendant(sourceDirectoryPath, targetParentDirectoryPath))
        {
            return new OperationResult(false, "Cannot move a directory into itself or its descendants.");
        }

        DirectoryEntity? source = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, sourceDirectoryPath);
        DirectoryEntity? targetParent = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, targetParentDirectoryPath);
        if (source is null || targetParent is null)
        {
            return new OperationResult(false, "Source or target directory not found.");
        }

        if (StorageNameConflictQueries.HasDirectoryNameConflict(_dbContext, targetParent.Id, source.Name, excludeDirectoryId: null))
        {
            return new OperationResult(false, $"Directory already exists: {source.Name}");
        }

        string sourcePhysicalPath = ResolvePhysicalPath(source.RelativePath);
        string targetParentPhysicalPath = ResolvePhysicalPath(targetParent.RelativePath);
        string destinationPath = Path.Combine(targetParentPhysicalPath, source.Name);
        OperationResult? sourcePathValidation = ValidatePhysicalDirectory(sourcePhysicalPath, "source");
        if (sourcePathValidation is not null)
        {
            return sourcePathValidation;
        }

        OperationResult? targetPathValidation = ValidatePhysicalDirectory(targetParentPhysicalPath, "target");
        if (targetPathValidation is not null)
        {
            return targetPathValidation;
        }

        try
        {
            Directory.Move(sourcePhysicalPath, destinationPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _auditTrailWriter.Write($"MOVE_DIRECTORY|{sourceDirectoryPath}|{targetParentDirectoryPath}|FAIL|{ex.Message}");
            return new OperationResult(false, ex.Message);
        }

        Guid? originalParentId = source.ParentId;
        string originalPhysicalPath = source.RelativePath;

        source.ParentId = targetParent.Id;
        source.RelativePath = ToStoredPath(destinationPath);
        StorageDirectoryMutations.UpdateDescendantPhysicalPaths(_dbContext, source.Id, source.RelativePath);

        return await ExecuteSaveWithRollbackAsync(
            restoreEntityState: () =>
            {
                source.ParentId = originalParentId;
                source.RelativePath = originalPhysicalPath;
                StorageDirectoryMutations.UpdateDescendantPhysicalPaths(_dbContext, source.Id, source.RelativePath);
            },
            rollbackPhysicalState: () => TryRollbackDirectoryMove(destinationPath, sourcePhysicalPath),
            successAuditEntry: $"MOVE_DIRECTORY|{sourceDirectoryPath}|{targetParentDirectoryPath}|SUCCESS",
            failureAuditPrefix: $"MOVE_DIRECTORY|{sourceDirectoryPath}|{targetParentDirectoryPath}|FAIL",
            successMessage: "Directory moved.",
            rollbackFailureMessage: "Database update failed after physical move. The operation was rolled back.",
            rollbackFailureErrorCode: OperationErrorCodes.MoveDirectoryUnexpected,
            cancellationToken: cancellationToken);
    }
}
