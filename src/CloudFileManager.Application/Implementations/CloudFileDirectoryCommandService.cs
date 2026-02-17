using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Interfaces;
using CloudFileManager.Application.Models;
using CloudFileManager.Domain;
using CloudFileManager.Shared.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CloudFileManager.Application.Implementations;

/// <summary>
/// CloudFileDirectoryCommandService 類別，負責目錄命令流程。
/// </summary>
public sealed class CloudFileDirectoryCommandService : ICloudFileDirectoryCommandService
{
    private static readonly Action<ILogger, string, string, Exception?> LogCreateDirectoryRequestedMessage =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(1101, nameof(CreateDirectory)), "Create directory requested. ParentPath={ParentPath}, DirectoryName={DirectoryName}");
    private static readonly Action<ILogger, string, Exception?> LogCreateParentNotFoundMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1102, nameof(CreateDirectory)), "Create directory failed because parent was not found. ParentPath={ParentPath}");
    private static readonly Action<ILogger, string, Exception?> LogDeleteDirectoryNotFoundMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1103, nameof(DeleteDirectory)), "Delete directory failed because directory was not found. DirectoryPath={DirectoryPath}");
    private static readonly Action<ILogger, string, Exception?> LogMoveSourceNotFoundMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1104, nameof(MoveDirectory)), "Move directory failed because source was not found. SourceDirectoryPath={SourceDirectoryPath}");
    private static readonly Action<ILogger, string, Exception?> LogMoveTargetParentNotFoundMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1105, nameof(MoveDirectory)), "Move directory failed because target parent was not found. TargetParentDirectoryPath={TargetParentDirectoryPath}");
    private static readonly Action<ILogger, string, Exception?> LogRenameDirectoryNotFoundMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1106, nameof(RenameDirectory)), "Rename directory failed because directory was not found. DirectoryPath={DirectoryPath}");

    private readonly CloudDirectory _root;
    private readonly IStorageMetadataGateway _storageMetadataGateway;
    private readonly AppConfig _config;
    private readonly ILogger<CloudFileDirectoryCommandService> _logger;

    /// <summary>
    /// 初始化 CloudFileDirectoryCommandService。
    /// </summary>
    public CloudFileDirectoryCommandService(
        CloudDirectory root,
        IStorageMetadataGateway storageMetadataGateway,
        AppConfig config,
        ILogger<CloudFileDirectoryCommandService>? logger = null)
    {
        _root = root;
        _storageMetadataGateway = storageMetadataGateway;
        _config = config;
        _logger = logger ?? NullLogger<CloudFileDirectoryCommandService>.Instance;
    }

    /// <summary>
    /// 建立目錄。
    /// </summary>
    public OperationResult CreateDirectory(CreateDirectoryRequest request)
    {
        LogCreateDirectoryRequestedMessage(_logger, request.ParentPath, request.DirectoryName, null);
        string? directoryNameError = NodeNameValidator.Validate(request.DirectoryName, "Directory");
        if (directoryNameError is not null)
        {
            return new OperationResult(false, directoryNameError, OperationErrorCodes.ValidationFailed);
        }

        CloudDirectory? parent = CloudFileTreeLookup.FindDirectory(_root, request.ParentPath);
        if (parent is null)
        {
            LogCreateParentNotFoundMessage(_logger, request.ParentPath, null);
            return new OperationResult(false, $"Parent directory not found: {request.ParentPath}", OperationErrorCodes.ResourceNotFound);
        }

        try
        {
            OperationResult persistenceResult = _storageMetadataGateway.CreateDirectory(request.ParentPath, request.DirectoryName);
            if (!persistenceResult.Success)
            {
                return persistenceResult;
            }

            parent.AddDirectory(request.DirectoryName, DateTime.UtcNow);
            return new OperationResult(true, "Directory created.");
        }
        catch (Exception ex)
        {
            return MapValidationOrUnexpectedError(ex, "Create directory failed due to an unexpected error.", OperationErrorCodes.CreateDirectoryUnexpected);
        }
    }

    public async Task<OperationResult> CreateDirectoryAsync(CreateDirectoryRequest request, CancellationToken cancellationToken = default)
    {
        LogCreateDirectoryRequestedMessage(_logger, request.ParentPath, request.DirectoryName, null);
        string? directoryNameError = NodeNameValidator.Validate(request.DirectoryName, "Directory");
        if (directoryNameError is not null)
        {
            return new OperationResult(false, directoryNameError, OperationErrorCodes.ValidationFailed);
        }

        CloudDirectory? parent = CloudFileTreeLookup.FindDirectory(_root, request.ParentPath);
        if (parent is null)
        {
            LogCreateParentNotFoundMessage(_logger, request.ParentPath, null);
            return new OperationResult(false, $"Parent directory not found: {request.ParentPath}", OperationErrorCodes.ResourceNotFound);
        }

        try
        {
            OperationResult persistenceResult = await _storageMetadataGateway.CreateDirectoryAsync(request.ParentPath, request.DirectoryName, cancellationToken);
            if (!persistenceResult.Success)
            {
                return persistenceResult;
            }

            parent.AddDirectory(request.DirectoryName, DateTime.UtcNow);
            return new OperationResult(true, "Directory created.");
        }
        catch (Exception ex)
        {
            return MapValidationOrUnexpectedError(ex, "Create directory failed due to an unexpected error.", OperationErrorCodes.CreateDirectoryUnexpected);
        }
    }

    /// <summary>
    /// 刪除目錄。
    /// </summary>
    public OperationResult DeleteDirectory(DeleteDirectoryRequest request)
    {
        if (string.Equals(request.DirectoryPath, "Root", StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResult(false, "Root directory cannot be deleted.", OperationErrorCodes.PolicyViolation);
        }

        var lookup = CloudFileTreeLookup.FindDirectoryWithParent(_root, request.DirectoryPath);
        if (lookup.Parent is null || lookup.Directory is null)
        {
            LogDeleteDirectoryNotFoundMessage(_logger, request.DirectoryPath, null);
            return new OperationResult(false, $"Directory not found: {request.DirectoryPath}", OperationErrorCodes.ResourceNotFound);
        }

        if (lookup.Directory.Directories.Count > 0 || lookup.Directory.Files.Count > 0)
        {
            if (_config.Management.GetDirectoryDeletePolicy() != DirectoryDeletePolicyType.RecursiveDelete)
            {
                return new OperationResult(false, "Directory is not empty and policy forbids delete.", OperationErrorCodes.PolicyViolation);
            }
        }

        OperationResult deletePersistenceResult = _storageMetadataGateway.DeleteDirectory(request.DirectoryPath);
        if (!deletePersistenceResult.Success)
        {
            return deletePersistenceResult;
        }

        bool removed = lookup.Parent.RemoveDirectory(lookup.Directory.Name);
        return removed
            ? new OperationResult(true, "Directory deleted.")
            : new OperationResult(false, "Unable to remove directory from memory tree.");
    }

    public async Task<OperationResult> DeleteDirectoryAsync(DeleteDirectoryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.Equals(request.DirectoryPath, "Root", StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResult(false, "Root directory cannot be deleted.", OperationErrorCodes.PolicyViolation);
        }

        var lookup = CloudFileTreeLookup.FindDirectoryWithParent(_root, request.DirectoryPath);
        if (lookup.Parent is null || lookup.Directory is null)
        {
            LogDeleteDirectoryNotFoundMessage(_logger, request.DirectoryPath, null);
            return new OperationResult(false, $"Directory not found: {request.DirectoryPath}", OperationErrorCodes.ResourceNotFound);
        }

        if (lookup.Directory.Directories.Count > 0 || lookup.Directory.Files.Count > 0)
        {
            if (_config.Management.GetDirectoryDeletePolicy() != DirectoryDeletePolicyType.RecursiveDelete)
            {
                return new OperationResult(false, "Directory is not empty and policy forbids delete.", OperationErrorCodes.PolicyViolation);
            }
        }

        OperationResult deletePersistenceResult = await _storageMetadataGateway.DeleteDirectoryAsync(request.DirectoryPath, cancellationToken);
        if (!deletePersistenceResult.Success)
        {
            return deletePersistenceResult;
        }

        bool removed = lookup.Parent.RemoveDirectory(lookup.Directory.Name);
        return removed
            ? new OperationResult(true, "Directory deleted.")
            : new OperationResult(false, "Unable to remove directory from memory tree.");
    }

    /// <summary>
    /// 搬移目錄。
    /// </summary>
    public OperationResult MoveDirectory(MoveDirectoryRequest request)
    {
        if (string.Equals(request.SourceDirectoryPath, "Root", StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResult(false, "Root directory cannot be moved.");
        }

        var sourceLookup = CloudFileTreeLookup.FindDirectoryWithParent(_root, request.SourceDirectoryPath);
        if (sourceLookup.Parent is null || sourceLookup.Directory is null)
        {
            LogMoveSourceNotFoundMessage(_logger, request.SourceDirectoryPath, null);
            return new OperationResult(false, $"Source directory not found: {request.SourceDirectoryPath}");
        }

        CloudDirectory? targetParent = CloudFileTreeLookup.FindDirectory(_root, request.TargetParentDirectoryPath);
        if (targetParent is null)
        {
            LogMoveTargetParentNotFoundMessage(_logger, request.TargetParentDirectoryPath, null);
            return new OperationResult(false, $"Target parent directory not found: {request.TargetParentDirectoryPath}");
        }

        if (PathHierarchyRules.IsSameOrDescendant(request.SourceDirectoryPath, request.TargetParentDirectoryPath))
        {
            return new OperationResult(false, "Cannot move a directory into itself or its descendants.");
        }

        OperationResult movePersistenceResult = _storageMetadataGateway.MoveDirectory(request.SourceDirectoryPath, request.TargetParentDirectoryPath);
        if (!movePersistenceResult.Success)
        {
            return movePersistenceResult;
        }

        CloudDirectory? detached = sourceLookup.Parent.DetachDirectory(sourceLookup.Directory.Name);
        if (detached is null)
        {
            return new OperationResult(false, "Unable to detach source directory.");
        }

        try
        {
            targetParent.AttachDirectory(detached);
        }
        catch (InvalidOperationException ex)
        {
            sourceLookup.Parent.AttachDirectory(detached);
            return new OperationResult(false, ex.Message);
        }
        catch
        {
            sourceLookup.Parent.AttachDirectory(detached);
            return new OperationResult(false, "Move directory failed due to an unexpected error.", OperationErrorCodes.MoveDirectoryUnexpected);
        }

        return new OperationResult(true, "Directory moved.");
    }

    public async Task<OperationResult> MoveDirectoryAsync(MoveDirectoryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.Equals(request.SourceDirectoryPath, "Root", StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResult(false, "Root directory cannot be moved.");
        }

        var sourceLookup = CloudFileTreeLookup.FindDirectoryWithParent(_root, request.SourceDirectoryPath);
        if (sourceLookup.Parent is null || sourceLookup.Directory is null)
        {
            LogMoveSourceNotFoundMessage(_logger, request.SourceDirectoryPath, null);
            return new OperationResult(false, $"Source directory not found: {request.SourceDirectoryPath}");
        }

        CloudDirectory? targetParent = CloudFileTreeLookup.FindDirectory(_root, request.TargetParentDirectoryPath);
        if (targetParent is null)
        {
            LogMoveTargetParentNotFoundMessage(_logger, request.TargetParentDirectoryPath, null);
            return new OperationResult(false, $"Target parent directory not found: {request.TargetParentDirectoryPath}");
        }

        if (PathHierarchyRules.IsSameOrDescendant(request.SourceDirectoryPath, request.TargetParentDirectoryPath))
        {
            return new OperationResult(false, "Cannot move a directory into itself or its descendants.");
        }

        OperationResult movePersistenceResult = await _storageMetadataGateway.MoveDirectoryAsync(request.SourceDirectoryPath, request.TargetParentDirectoryPath, cancellationToken);
        if (!movePersistenceResult.Success)
        {
            return movePersistenceResult;
        }

        CloudDirectory? detached = sourceLookup.Parent.DetachDirectory(sourceLookup.Directory.Name);
        if (detached is null)
        {
            return new OperationResult(false, "Unable to detach source directory.");
        }

        try
        {
            targetParent.AttachDirectory(detached);
        }
        catch (InvalidOperationException ex)
        {
            sourceLookup.Parent.AttachDirectory(detached);
            return new OperationResult(false, ex.Message);
        }
        catch
        {
            sourceLookup.Parent.AttachDirectory(detached);
            return new OperationResult(false, "Move directory failed due to an unexpected error.", OperationErrorCodes.MoveDirectoryUnexpected);
        }

        return new OperationResult(true, "Directory moved.");
    }

    /// <summary>
    /// 重新命名目錄。
    /// </summary>
    public OperationResult RenameDirectory(RenameDirectoryRequest request)
    {
        string? directoryNameError = NodeNameValidator.Validate(request.NewDirectoryName, "Directory");
        if (directoryNameError is not null)
        {
            return new OperationResult(false, directoryNameError);
        }

        if (string.Equals(request.DirectoryPath, "Root", StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResult(false, "Root directory cannot be renamed.");
        }

        var lookup = CloudFileTreeLookup.FindDirectoryWithParent(_root, request.DirectoryPath);
        if (lookup.Parent is null || lookup.Directory is null)
        {
            LogRenameDirectoryNotFoundMessage(_logger, request.DirectoryPath, null);
            return new OperationResult(false, $"Directory not found: {request.DirectoryPath}");
        }

        if (lookup.Directory.Name.Equals(request.NewDirectoryName, StringComparison.Ordinal))
        {
            return new OperationResult(true, "Directory renamed.");
        }

        if (lookup.Parent.Directories.Any(item => !ReferenceEquals(item, lookup.Directory) && item.Name.Equals(request.NewDirectoryName, StringComparison.OrdinalIgnoreCase)))
        {
            return new OperationResult(false, $"Directory already exists: {request.NewDirectoryName}");
        }

        OperationResult renamePersistenceResult = _storageMetadataGateway.RenameDirectory(request.DirectoryPath, request.NewDirectoryName);
        if (!renamePersistenceResult.Success)
        {
            return renamePersistenceResult;
        }

        lookup.Directory.Rename(request.NewDirectoryName);
        return new OperationResult(true, "Directory renamed.");
    }

    public async Task<OperationResult> RenameDirectoryAsync(RenameDirectoryRequest request, CancellationToken cancellationToken = default)
    {
        string? directoryNameError = NodeNameValidator.Validate(request.NewDirectoryName, "Directory");
        if (directoryNameError is not null)
        {
            return new OperationResult(false, directoryNameError);
        }

        if (string.Equals(request.DirectoryPath, "Root", StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResult(false, "Root directory cannot be renamed.");
        }

        var lookup = CloudFileTreeLookup.FindDirectoryWithParent(_root, request.DirectoryPath);
        if (lookup.Parent is null || lookup.Directory is null)
        {
            LogRenameDirectoryNotFoundMessage(_logger, request.DirectoryPath, null);
            return new OperationResult(false, $"Directory not found: {request.DirectoryPath}");
        }

        if (lookup.Directory.Name.Equals(request.NewDirectoryName, StringComparison.Ordinal))
        {
            return new OperationResult(true, "Directory renamed.");
        }

        if (lookup.Parent.Directories.Any(item => !ReferenceEquals(item, lookup.Directory) && item.Name.Equals(request.NewDirectoryName, StringComparison.OrdinalIgnoreCase)))
        {
            return new OperationResult(false, $"Directory already exists: {request.NewDirectoryName}");
        }

        OperationResult renamePersistenceResult = await _storageMetadataGateway.RenameDirectoryAsync(request.DirectoryPath, request.NewDirectoryName, cancellationToken);
        if (!renamePersistenceResult.Success)
        {
            return renamePersistenceResult;
        }

        lookup.Directory.Rename(request.NewDirectoryName);
        return new OperationResult(true, "Directory renamed.");
    }

    public OperationResult CopyDirectory(CopyDirectoryRequest request)
    {
        if (string.Equals(request.SourceDirectoryPath, "Root", StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResult(false, "Root directory cannot be copied.", OperationErrorCodes.PolicyViolation);
        }

        CloudDirectory? sourceDirectory = CloudFileTreeLookup.FindDirectory(_root, request.SourceDirectoryPath);
        if (sourceDirectory is null)
        {
            return new OperationResult(false, $"Source directory not found: {request.SourceDirectoryPath}", OperationErrorCodes.ResourceNotFound);
        }

        CloudDirectory? targetParent = CloudFileTreeLookup.FindDirectory(_root, request.TargetParentDirectoryPath);
        if (targetParent is null)
        {
            return new OperationResult(false, $"Target parent directory not found: {request.TargetParentDirectoryPath}", OperationErrorCodes.ResourceNotFound);
        }

        string newDirectoryName = string.IsNullOrWhiteSpace(request.NewDirectoryName)
            ? sourceDirectory.Name
            : request.NewDirectoryName.Trim();

        string? directoryNameError = NodeNameValidator.Validate(newDirectoryName, "Directory");
        if (directoryNameError is not null)
        {
            return new OperationResult(false, directoryNameError, OperationErrorCodes.ValidationFailed);
        }

        if (targetParent.Directories.Any(item => item.Name.Equals(newDirectoryName, StringComparison.OrdinalIgnoreCase)) ||
            targetParent.Files.Any(item => item.Name.Equals(newDirectoryName, StringComparison.OrdinalIgnoreCase)))
        {
            return new OperationResult(false, $"Name conflict: {newDirectoryName}", OperationErrorCodes.NameConflict);
        }

        string targetPath = request.TargetParentDirectoryPath.TrimEnd('/');
        string sourcePath = request.SourceDirectoryPath.TrimEnd('/');
        List<string> createdFilePaths = [];
        List<string> createdDirectoryPaths = [];
        OperationResult result = CopyDirectoryRecursive(sourceDirectory, sourcePath, targetParent, targetPath, newDirectoryName, createdFilePaths, createdDirectoryPaths);
        if (result.Success)
        {
            return result;
        }

        _ = targetParent.RemoveDirectory(newDirectoryName);
        OperationResult rollbackResult = RollbackCopiedPaths(createdFilePaths, createdDirectoryPaths);
        return MergeCopyFailureResult(result, rollbackResult);
    }

    public async Task<OperationResult> CopyDirectoryAsync(CopyDirectoryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.Equals(request.SourceDirectoryPath, "Root", StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResult(false, "Root directory cannot be copied.", OperationErrorCodes.PolicyViolation);
        }

        CloudDirectory? sourceDirectory = CloudFileTreeLookup.FindDirectory(_root, request.SourceDirectoryPath);
        if (sourceDirectory is null)
        {
            return new OperationResult(false, $"Source directory not found: {request.SourceDirectoryPath}", OperationErrorCodes.ResourceNotFound);
        }

        CloudDirectory? targetParent = CloudFileTreeLookup.FindDirectory(_root, request.TargetParentDirectoryPath);
        if (targetParent is null)
        {
            return new OperationResult(false, $"Target parent directory not found: {request.TargetParentDirectoryPath}", OperationErrorCodes.ResourceNotFound);
        }

        string newDirectoryName = string.IsNullOrWhiteSpace(request.NewDirectoryName)
            ? sourceDirectory.Name
            : request.NewDirectoryName.Trim();

        string? directoryNameError = NodeNameValidator.Validate(newDirectoryName, "Directory");
        if (directoryNameError is not null)
        {
            return new OperationResult(false, directoryNameError, OperationErrorCodes.ValidationFailed);
        }

        if (targetParent.Directories.Any(item => item.Name.Equals(newDirectoryName, StringComparison.OrdinalIgnoreCase)) ||
            targetParent.Files.Any(item => item.Name.Equals(newDirectoryName, StringComparison.OrdinalIgnoreCase)))
        {
            return new OperationResult(false, $"Name conflict: {newDirectoryName}", OperationErrorCodes.NameConflict);
        }

        string targetPath = request.TargetParentDirectoryPath.TrimEnd('/');
        string sourcePath = request.SourceDirectoryPath.TrimEnd('/');
        List<string> createdFilePaths = [];
        List<string> createdDirectoryPaths = [];
        OperationResult result = await CopyDirectoryRecursiveAsync(sourceDirectory, sourcePath, targetParent, targetPath, newDirectoryName, createdFilePaths, createdDirectoryPaths, cancellationToken);
        if (result.Success)
        {
            return result;
        }

        _ = targetParent.RemoveDirectory(newDirectoryName);
        OperationResult rollbackResult = await RollbackCopiedPathsAsync(createdFilePaths, createdDirectoryPaths, cancellationToken);
        return MergeCopyFailureResult(result, rollbackResult);
    }

    private OperationResult CopyDirectoryRecursive(
        CloudDirectory sourceDirectory,
        string sourceDirectoryPath,
        CloudDirectory targetParent,
        string targetParentPath,
        string newDirectoryName,
        ICollection<string> createdFilePaths,
        ICollection<string> createdDirectoryPaths)
    {
        OperationResult createPersistenceResult = _storageMetadataGateway.CreateDirectory(targetParentPath, newDirectoryName);
        if (!createPersistenceResult.Success)
        {
            return createPersistenceResult;
        }

        string clonedPath = $"{targetParentPath}/{newDirectoryName}";
        createdDirectoryPaths.Add(clonedPath);

        CloudDirectory clonedDirectory;
        try
        {
            clonedDirectory = targetParent.AddDirectory(newDirectoryName, DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return new OperationResult(false, ex.Message, OperationErrorCodes.NameConflict);
        }

        foreach (CloudFile sourceFile in sourceDirectory.Files)
        {
            if (clonedDirectory.Files.Any(item => item.Name.Equals(sourceFile.Name, StringComparison.OrdinalIgnoreCase)) ||
                clonedDirectory.Directories.Any(item => item.Name.Equals(sourceFile.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return new OperationResult(false, $"Name conflict: {sourceFile.Name}", OperationErrorCodes.NameConflict);
            }

            string sourceFilePath = $"{sourceDirectoryPath}/{sourceFile.Name}";
            string? temporarySourcePath = null;
            OperationResult uploadResult;
            try
            {
                temporarySourcePath = CreateTemporarySourceCopyIfAvailable(sourceFilePath, sourceFile.Name);
                uploadResult = _storageMetadataGateway.UploadFile(
                    BuildCopyUploadRequest(clonedPath, sourceFile, temporarySourcePath),
                    sourceFile.FileType);
            }
            finally
            {
                TryDeleteTemporaryFile(temporarySourcePath);
            }

            if (!uploadResult.Success)
            {
                return uploadResult;
            }

            createdFilePaths.Add($"{clonedPath}/{sourceFile.Name}");

            try
            {
                clonedDirectory.AddFile(CloneFile(sourceFile));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return new OperationResult(false, ex.Message, OperationErrorCodes.NameConflict);
            }
        }

        foreach (CloudDirectory childDirectory in sourceDirectory.Directories)
        {
            string childSourcePath = $"{sourceDirectoryPath}/{childDirectory.Name}";
            OperationResult childCopyResult = CopyDirectoryRecursive(childDirectory, childSourcePath, clonedDirectory, clonedPath, childDirectory.Name, createdFilePaths, createdDirectoryPaths);
            if (!childCopyResult.Success)
            {
                return childCopyResult;
            }
        }

        return new OperationResult(true, "Directory copied.");
    }

    private async Task<OperationResult> CopyDirectoryRecursiveAsync(
        CloudDirectory sourceDirectory,
        string sourceDirectoryPath,
        CloudDirectory targetParent,
        string targetParentPath,
        string newDirectoryName,
        ICollection<string> createdFilePaths,
        ICollection<string> createdDirectoryPaths,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        OperationResult createPersistenceResult = await _storageMetadataGateway.CreateDirectoryAsync(targetParentPath, newDirectoryName, cancellationToken);
        if (!createPersistenceResult.Success)
        {
            return createPersistenceResult;
        }

        string clonedPath = $"{targetParentPath}/{newDirectoryName}";
        createdDirectoryPaths.Add(clonedPath);

        CloudDirectory clonedDirectory;
        try
        {
            clonedDirectory = targetParent.AddDirectory(newDirectoryName, DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return new OperationResult(false, ex.Message, OperationErrorCodes.NameConflict);
        }

        foreach (CloudFile sourceFile in sourceDirectory.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (clonedDirectory.Files.Any(item => item.Name.Equals(sourceFile.Name, StringComparison.OrdinalIgnoreCase)) ||
                clonedDirectory.Directories.Any(item => item.Name.Equals(sourceFile.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return new OperationResult(false, $"Name conflict: {sourceFile.Name}", OperationErrorCodes.NameConflict);
            }

            string sourceFilePath = $"{sourceDirectoryPath}/{sourceFile.Name}";
            string? temporarySourcePath = null;
            OperationResult uploadResult;
            try
            {
                temporarySourcePath = await CreateTemporarySourceCopyIfAvailableAsync(sourceFilePath, sourceFile.Name, cancellationToken);
                uploadResult = await _storageMetadataGateway.UploadFileAsync(
                    BuildCopyUploadRequest(clonedPath, sourceFile, temporarySourcePath),
                    sourceFile.FileType,
                    cancellationToken);
            }
            finally
            {
                TryDeleteTemporaryFile(temporarySourcePath);
            }

            if (!uploadResult.Success)
            {
                return uploadResult;
            }

            createdFilePaths.Add($"{clonedPath}/{sourceFile.Name}");

            try
            {
                clonedDirectory.AddFile(CloneFile(sourceFile));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return new OperationResult(false, ex.Message, OperationErrorCodes.NameConflict);
            }
        }

        foreach (CloudDirectory childDirectory in sourceDirectory.Directories)
        {
            string childSourcePath = $"{sourceDirectoryPath}/{childDirectory.Name}";
            OperationResult childCopyResult = await CopyDirectoryRecursiveAsync(
                childDirectory,
                childSourcePath,
                clonedDirectory,
                clonedPath,
                childDirectory.Name,
                createdFilePaths,
                createdDirectoryPaths,
                cancellationToken);
            if (!childCopyResult.Success)
            {
                return childCopyResult;
            }
        }

        return new OperationResult(true, "Directory copied.");
    }

    private static UploadFileRequest BuildCopyUploadRequest(string targetDirectoryPath, CloudFile sourceFile, string? sourceLocalPath)
    {
        return sourceFile switch
        {
            WordFile wordFile => new UploadFileRequest(targetDirectoryPath, sourceFile.Name, sourceFile.Size, PageCount: wordFile.PageCount, SourceLocalPath: sourceLocalPath),
            ImageFile imageFile => new UploadFileRequest(targetDirectoryPath, sourceFile.Name, sourceFile.Size, Width: imageFile.Width, Height: imageFile.Height, SourceLocalPath: sourceLocalPath),
            TextFile textFile => new UploadFileRequest(targetDirectoryPath, sourceFile.Name, sourceFile.Size, Encoding: textFile.Encoding, SourceLocalPath: sourceLocalPath),
            _ => new UploadFileRequest(targetDirectoryPath, sourceFile.Name, sourceFile.Size, SourceLocalPath: sourceLocalPath)
        };
    }

    private string? CreateTemporarySourceCopyIfAvailable(string sourceFilePath, string sourceFileName)
    {
        FileDownloadResult downloadResult = _storageMetadataGateway.DownloadFileContent(sourceFilePath);
        if (!downloadResult.Success || downloadResult.Content is null)
        {
            return null;
        }

        string extension = Path.GetExtension(sourceFileName);
        string tempPath = Path.Combine(Path.GetTempPath(), $"cfm-copy-{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(tempPath, downloadResult.Content);
        return tempPath;
    }

    private async Task<string?> CreateTemporarySourceCopyIfAvailableAsync(string sourceFilePath, string sourceFileName, CancellationToken cancellationToken)
    {
        FileDownloadResult downloadResult = await _storageMetadataGateway.DownloadFileContentAsync(sourceFilePath, cancellationToken);
        if (!downloadResult.Success || downloadResult.Content is null)
        {
            return null;
        }

        string extension = Path.GetExtension(sourceFileName);
        string tempPath = Path.Combine(Path.GetTempPath(), $"cfm-copy-{Guid.NewGuid():N}{extension}");
        await File.WriteAllBytesAsync(tempPath, downloadResult.Content, cancellationToken);
        return tempPath;
    }

    private OperationResult RollbackCopiedPaths(ICollection<string> createdFilePaths, ICollection<string> createdDirectoryPaths)
    {
        bool rollbackSucceeded = true;

        foreach (string filePath in createdFilePaths.Reverse())
        {
            OperationResult deleteFileResult = _storageMetadataGateway.DeleteFile(filePath);
            rollbackSucceeded &= deleteFileResult.Success;
        }

        foreach (string directoryPath in createdDirectoryPaths.OrderByDescending(path => path.Length))
        {
            OperationResult deleteDirectoryResult = _storageMetadataGateway.DeleteDirectory(directoryPath);
            rollbackSucceeded &= deleteDirectoryResult.Success;
        }

        return rollbackSucceeded
            ? new OperationResult(false, "Directory copy failed and partial changes were rolled back.", OperationErrorCodes.CopyDirectoryUnexpected)
            : new OperationResult(false, "Directory copy failed and rollback was incomplete. Manual intervention is required.", OperationErrorCodes.CopyDirectoryRollbackFailed);
    }

    private async Task<OperationResult> RollbackCopiedPathsAsync(ICollection<string> createdFilePaths, ICollection<string> createdDirectoryPaths, CancellationToken cancellationToken)
    {
        bool rollbackSucceeded = true;

        foreach (string filePath in createdFilePaths.Reverse())
        {
            OperationResult deleteFileResult = await _storageMetadataGateway.DeleteFileAsync(filePath, cancellationToken);
            rollbackSucceeded &= deleteFileResult.Success;
        }

        foreach (string directoryPath in createdDirectoryPaths.OrderByDescending(path => path.Length))
        {
            OperationResult deleteDirectoryResult = await _storageMetadataGateway.DeleteDirectoryAsync(directoryPath, cancellationToken);
            rollbackSucceeded &= deleteDirectoryResult.Success;
        }

        return rollbackSucceeded
            ? new OperationResult(false, "Directory copy failed and partial changes were rolled back.", OperationErrorCodes.CopyDirectoryUnexpected)
            : new OperationResult(false, "Directory copy failed and rollback was incomplete. Manual intervention is required.", OperationErrorCodes.CopyDirectoryRollbackFailed);
    }

    private static OperationResult MergeCopyFailureResult(OperationResult originalFailure, OperationResult rollbackResult)
    {
        if (rollbackResult.ErrorCode == OperationErrorCodes.CopyDirectoryRollbackFailed)
        {
            return rollbackResult;
        }

        string errorCode = string.IsNullOrWhiteSpace(originalFailure.ErrorCode)
            ? OperationErrorCodes.CopyDirectoryUnexpected
            : originalFailure.ErrorCode;

        return new OperationResult(false, originalFailure.Message, errorCode);
    }

    private static void TryDeleteTemporaryFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore temporary cleanup failures.
        }
    }

    private static CloudFile CloneFile(CloudFile sourceFile)
    {
        return sourceFile switch
        {
            WordFile wordFile => new WordFile(wordFile.Name, wordFile.Size, DateTime.UtcNow, wordFile.PageCount),
            ImageFile imageFile => new ImageFile(imageFile.Name, imageFile.Size, DateTime.UtcNow, imageFile.Width, imageFile.Height),
            TextFile textFile => new TextFile(textFile.Name, textFile.Size, DateTime.UtcNow, textFile.Encoding),
            _ => throw new InvalidOperationException($"Unsupported file type: {sourceFile.GetType().Name}")
        };
    }

    private static OperationResult MapValidationOrUnexpectedError(Exception ex, string unexpectedMessage, string unexpectedErrorCode)
    {
        return ex switch
        {
            InvalidOperationException => new OperationResult(false, ex.Message, OperationErrorCodes.ValidationFailed),
            ArgumentException => new OperationResult(false, ex.Message, OperationErrorCodes.ValidationFailed),
            _ => new OperationResult(false, unexpectedMessage, unexpectedErrorCode)
        };
    }
}
