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
            return new OperationResult(false, directoryNameError);
        }

        CloudDirectory? parent = CloudFileTreeLookup.FindDirectory(_root, request.ParentPath);
        if (parent is null)
        {
            LogCreateParentNotFoundMessage(_logger, request.ParentPath, null);
            return new OperationResult(false, $"Parent directory not found: {request.ParentPath}");
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
        catch (InvalidOperationException ex)
        {
            return new OperationResult(false, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return new OperationResult(false, ex.Message);
        }
        catch
        {
            return new OperationResult(false, "Create directory failed due to an unexpected error.", OperationErrorCodes.CreateDirectoryUnexpected);
        }
    }

    public async Task<OperationResult> CreateDirectoryAsync(CreateDirectoryRequest request, CancellationToken cancellationToken = default)
    {
        LogCreateDirectoryRequestedMessage(_logger, request.ParentPath, request.DirectoryName, null);
        string? directoryNameError = NodeNameValidator.Validate(request.DirectoryName, "Directory");
        if (directoryNameError is not null)
        {
            return new OperationResult(false, directoryNameError);
        }

        CloudDirectory? parent = CloudFileTreeLookup.FindDirectory(_root, request.ParentPath);
        if (parent is null)
        {
            LogCreateParentNotFoundMessage(_logger, request.ParentPath, null);
            return new OperationResult(false, $"Parent directory not found: {request.ParentPath}");
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
        catch (InvalidOperationException ex)
        {
            return new OperationResult(false, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return new OperationResult(false, ex.Message);
        }
        catch
        {
            return new OperationResult(false, "Create directory failed due to an unexpected error.", OperationErrorCodes.CreateDirectoryUnexpected);
        }
    }

    /// <summary>
    /// 刪除目錄。
    /// </summary>
    public OperationResult DeleteDirectory(DeleteDirectoryRequest request)
    {
        if (string.Equals(request.DirectoryPath, "Root", StringComparison.OrdinalIgnoreCase))
        {
            return new OperationResult(false, "Root directory cannot be deleted.");
        }

        var lookup = CloudFileTreeLookup.FindDirectoryWithParent(_root, request.DirectoryPath);
        if (lookup.Parent is null || lookup.Directory is null)
        {
            LogDeleteDirectoryNotFoundMessage(_logger, request.DirectoryPath, null);
            return new OperationResult(false, $"Directory not found: {request.DirectoryPath}");
        }

        if (lookup.Directory.Directories.Count > 0 || lookup.Directory.Files.Count > 0)
        {
            if (_config.Management.GetDirectoryDeletePolicy() != DirectoryDeletePolicyType.RecursiveDelete)
            {
                return new OperationResult(false, "Directory is not empty and policy forbids delete.");
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
            return new OperationResult(false, "Root directory cannot be deleted.");
        }

        var lookup = CloudFileTreeLookup.FindDirectoryWithParent(_root, request.DirectoryPath);
        if (lookup.Parent is null || lookup.Directory is null)
        {
            LogDeleteDirectoryNotFoundMessage(_logger, request.DirectoryPath, null);
            return new OperationResult(false, $"Directory not found: {request.DirectoryPath}");
        }

        if (lookup.Directory.Directories.Count > 0 || lookup.Directory.Files.Count > 0)
        {
            if (_config.Management.GetDirectoryDeletePolicy() != DirectoryDeletePolicyType.RecursiveDelete)
            {
                return new OperationResult(false, "Directory is not empty and policy forbids delete.");
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
}
