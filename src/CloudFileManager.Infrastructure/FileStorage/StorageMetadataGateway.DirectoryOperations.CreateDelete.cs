using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Models;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using CloudFileManager.Shared.Common;

namespace CloudFileManager.Infrastructure.FileStorage;

public sealed partial class StorageMetadataGateway
{
    public OperationResult CreateDirectory(string parentPath, string directoryName)
    {
        DirectoryEntity? parentDirectory = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, parentPath);
        if (parentDirectory is null)
        {
            return new OperationResult(false, $"Parent directory not found: {parentPath}");
        }

        bool duplicated = StorageNameConflictQueries.HasDirectoryNameConflict(_dbContext, parentDirectory.Id, directoryName, excludeDirectoryId: null);
        if (duplicated)
        {
            return new OperationResult(false, $"Directory already exists: {directoryName}");
        }

        string parentPhysicalPath = ResolvePhysicalPath(parentDirectory.RelativePath);
        string fullPath = Path.Combine(parentPhysicalPath, directoryName);
        try
        {
            Directory.CreateDirectory(fullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _auditTrailWriter.Write($"CREATE_DIRECTORY|{parentPath}|{directoryName}|FAIL|{ex.Message}");
            return new OperationResult(false, ex.Message);
        }

        int order = _dbContext.Directories.Count(item => item.ParentId == parentDirectory.Id) + 1;
        _dbContext.Directories.Add(new DirectoryEntity
        {
            Id = Guid.NewGuid(),
            ParentId = parentDirectory.Id,
            Name = directoryName,
            CreatedTime = DateTime.UtcNow,
            CreationOrder = order,
            RelativePath = ToStoredPath(fullPath)
        });

        OperationResult persistenceResult = ExecuteSaveWithRollback(
            restoreEntityState: () => _dbContext.ChangeTracker.Clear(),
            rollbackPhysicalState: () => DeleteDirectoryIfExists(fullPath),
            successAuditEntry: $"CREATE_DIRECTORY|{parentPath}|{directoryName}|SUCCESS",
            failureAuditPrefix: $"CREATE_DIRECTORY|{parentPath}|{directoryName}|FAIL",
            successMessage: "Directory persisted.",
            rollbackFailureMessage: "Database update failed after physical create. The operation was rolled back.",
            rollbackFailureErrorCode: OperationErrorCodes.CreateDirectoryUnexpected);

        if (persistenceResult.Success)
        {
            InvalidateRootTreeCache();
        }

        return persistenceResult;
    }

    public Task<OperationResult> CreateDirectoryAsync(string parentPath, string directoryName, CancellationToken cancellationToken = default)
    {
        return CreateDirectoryCoreAsync(parentPath, directoryName, cancellationToken);
    }

    private async Task<OperationResult> CreateDirectoryCoreAsync(string parentPath, string directoryName, CancellationToken cancellationToken)
    {
        DirectoryEntity? parentDirectory = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, parentPath);
        if (parentDirectory is null)
        {
            return new OperationResult(false, $"Parent directory not found: {parentPath}");
        }

        bool duplicated = StorageNameConflictQueries.HasDirectoryNameConflict(_dbContext, parentDirectory.Id, directoryName, excludeDirectoryId: null);
        if (duplicated)
        {
            return new OperationResult(false, $"Directory already exists: {directoryName}");
        }

        string parentPhysicalPath = ResolvePhysicalPath(parentDirectory.RelativePath);
        string fullPath = Path.Combine(parentPhysicalPath, directoryName);
        try
        {
            Directory.CreateDirectory(fullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _auditTrailWriter.Write($"CREATE_DIRECTORY|{parentPath}|{directoryName}|FAIL|{ex.Message}");
            return new OperationResult(false, ex.Message);
        }

        int order = _dbContext.Directories.Count(item => item.ParentId == parentDirectory.Id) + 1;
        _dbContext.Directories.Add(new DirectoryEntity
        {
            Id = Guid.NewGuid(),
            ParentId = parentDirectory.Id,
            Name = directoryName,
            CreatedTime = DateTime.UtcNow,
            CreationOrder = order,
            RelativePath = ToStoredPath(fullPath)
        });

        OperationResult persistenceResult = await ExecuteSaveWithRollbackAsync(
            restoreEntityState: () => _dbContext.ChangeTracker.Clear(),
            rollbackPhysicalState: () => DeleteDirectoryIfExists(fullPath),
            successAuditEntry: $"CREATE_DIRECTORY|{parentPath}|{directoryName}|SUCCESS",
            failureAuditPrefix: $"CREATE_DIRECTORY|{parentPath}|{directoryName}|FAIL",
            successMessage: "Directory persisted.",
            rollbackFailureMessage: "Database update failed after physical create. The operation was rolled back.",
            rollbackFailureErrorCode: OperationErrorCodes.CreateDirectoryUnexpected,
            cancellationToken: cancellationToken);

        if (persistenceResult.Success)
        {
            InvalidateRootTreeCache();
        }

        return persistenceResult;
    }

    public OperationResult DeleteDirectory(string directoryPath)
    {
        if (IsRootDirectoryPath(directoryPath))
        {
            return new OperationResult(false, "Root directory cannot be deleted.");
        }

        DirectoryEntity? directory = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, directoryPath);
        if (directory is null)
        {
            return new OperationResult(false, $"Directory not found: {directoryPath}");
        }

        if (HasChildNodes(directory.Id) && _management.GetDirectoryDeletePolicy() != DirectoryDeletePolicyType.RecursiveDelete)
        {
            return new OperationResult(false, "Directory is not empty and policy forbids delete.");
        }

        string sourcePath = ResolvePhysicalPath(directory.RelativePath);
        string pendingDeletePath = BuildPendingDeletePath(sourcePath);

        if (Directory.Exists(sourcePath))
        {
            try
            {
                Directory.Move(sourcePath, pendingDeletePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _auditTrailWriter.Write($"DELETE_DIRECTORY|{directoryPath}|FAIL|{ex.Message}");
                return new OperationResult(false, ex.Message);
            }
        }

        StorageDirectoryMutations.RemoveDirectoryCascade(_dbContext, directory.Id);
        OperationResult persistenceResult = ExecuteSaveWithRollback(
            restoreEntityState: () => _dbContext.ChangeTracker.Clear(),
            rollbackPhysicalState: () => TryRestoreMovedDirectory(pendingDeletePath, sourcePath),
            successAuditEntry: $"DELETE_DIRECTORY|{directoryPath}|SUCCESS",
            failureAuditPrefix: $"DELETE_DIRECTORY|{directoryPath}|FAIL",
            successMessage: "Directory deleted.",
            rollbackFailureMessage: "Database update failed after physical delete. The operation was rolled back.",
            rollbackFailureErrorCode: OperationErrorCodes.DeleteDirectoryUnexpected);

        if (!persistenceResult.Success)
        {
            return persistenceResult;
        }

        if (!DeleteDirectoryIfExists(pendingDeletePath))
        {
            _auditTrailWriter.Write($"DELETE_DIRECTORY|{directoryPath}|CLEANUP_FAIL");
            return new OperationResult(
                false,
                "Directory metadata was deleted, but physical cleanup did not complete.",
                OperationErrorCodes.DeleteDirectoryCleanupFailed);
        }

        InvalidateRootTreeCache();
        return persistenceResult;
    }

    public Task<OperationResult> DeleteDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        return DeleteDirectoryCoreAsync(directoryPath, cancellationToken);
    }

    private async Task<OperationResult> DeleteDirectoryCoreAsync(string directoryPath, CancellationToken cancellationToken)
    {
        if (IsRootDirectoryPath(directoryPath))
        {
            return new OperationResult(false, "Root directory cannot be deleted.");
        }

        DirectoryEntity? directory = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, directoryPath);
        if (directory is null)
        {
            return new OperationResult(false, $"Directory not found: {directoryPath}");
        }

        if (HasChildNodes(directory.Id) && _management.GetDirectoryDeletePolicy() != DirectoryDeletePolicyType.RecursiveDelete)
        {
            return new OperationResult(false, "Directory is not empty and policy forbids delete.");
        }

        string sourcePath = ResolvePhysicalPath(directory.RelativePath);
        string pendingDeletePath = BuildPendingDeletePath(sourcePath);

        if (Directory.Exists(sourcePath))
        {
            try
            {
                Directory.Move(sourcePath, pendingDeletePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _auditTrailWriter.Write($"DELETE_DIRECTORY|{directoryPath}|FAIL|{ex.Message}");
                return new OperationResult(false, ex.Message);
            }
        }

        StorageDirectoryMutations.RemoveDirectoryCascade(_dbContext, directory.Id);
        OperationResult persistenceResult = await ExecuteSaveWithRollbackAsync(
            restoreEntityState: () => _dbContext.ChangeTracker.Clear(),
            rollbackPhysicalState: () => TryRestoreMovedDirectory(pendingDeletePath, sourcePath),
            successAuditEntry: $"DELETE_DIRECTORY|{directoryPath}|SUCCESS",
            failureAuditPrefix: $"DELETE_DIRECTORY|{directoryPath}|FAIL",
            successMessage: "Directory deleted.",
            rollbackFailureMessage: "Database update failed after physical delete. The operation was rolled back.",
            rollbackFailureErrorCode: OperationErrorCodes.DeleteDirectoryUnexpected,
            cancellationToken: cancellationToken);

        if (!persistenceResult.Success)
        {
            return persistenceResult;
        }

        if (!DeleteDirectoryIfExists(pendingDeletePath))
        {
            _auditTrailWriter.Write($"DELETE_DIRECTORY|{directoryPath}|CLEANUP_FAIL");
            return new OperationResult(
                false,
                "Directory metadata was deleted, but physical cleanup did not complete.",
                OperationErrorCodes.DeleteDirectoryCleanupFailed);
        }

        InvalidateRootTreeCache();
        return persistenceResult;
    }

    private static bool IsRootDirectoryPath(string path)
    {
        return string.Equals(path, "Root", StringComparison.OrdinalIgnoreCase);
    }

    private bool HasChildNodes(Guid directoryId)
    {
        return _dbContext.Directories.Any(item => item.ParentId == directoryId)
            || _dbContext.Files.Any(item => item.DirectoryId == directoryId);
    }

    private static string BuildPendingDeletePath(string sourcePath)
    {
        string parentPath = Directory.GetParent(sourcePath)?.FullName ?? Path.GetTempPath();
        string name = Path.GetFileName(sourcePath);
        return Path.Combine(parentPath, $"{name}.deleting-{Guid.NewGuid():N}");
    }

    private static bool TryRestoreMovedDirectory(string sourcePath, string targetPath)
    {
        try
        {
            if (!Directory.Exists(sourcePath))
            {
                return false;
            }

            if (Directory.Exists(targetPath))
            {
                return false;
            }

            string? parentPath = Directory.GetParent(targetPath)?.FullName;
            if (!string.IsNullOrWhiteSpace(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }

            Directory.Move(sourcePath, targetPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool DeleteDirectoryIfExists(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
