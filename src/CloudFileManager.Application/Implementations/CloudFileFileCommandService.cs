using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Interfaces;
using CloudFileManager.Application.Models;
using CloudFileManager.Domain;
using CloudFileManager.Domain.Enums;
using CloudFileManager.Shared.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CloudFileManager.Application.Implementations;

/// <summary>
/// CloudFileFileCommandService 類別，負責檔案命令流程。
/// </summary>
public sealed class CloudFileFileCommandService : ICloudFileFileCommandService
{
    private static readonly Action<ILogger, string, string, Exception?> LogUploadRequestedMessage =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(1001, nameof(UploadFile)), "Upload file requested. DirectoryPath={DirectoryPath}, FileName={FileName}");
    private static readonly Action<ILogger, string, string, string, Exception?> LogUploadValidationFailedMessage =
        LoggerMessage.Define<string, string, string>(LogLevel.Warning, new EventId(1002, nameof(UploadFile)), "Upload file validation failed. DirectoryPath={DirectoryPath}, FileName={FileName}, Message={Message}");
    private static readonly Action<ILogger, string, string, string?, Exception?> LogUploadPersistenceFailedMessage =
        LoggerMessage.Define<string, string, string?>(LogLevel.Warning, new EventId(1003, nameof(UploadFile)), "Upload file persistence failed. DirectoryPath={DirectoryPath}, FileName={FileName}, ErrorCode={ErrorCode}");
    private static readonly Action<ILogger, string, Exception?> LogDownloadFileNotFoundMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1004, nameof(DownloadFile)), "Download file failed because file was not found. FilePath={FilePath}");
    private static readonly Action<ILogger, string, Exception?> LogDownloadContentNotFoundMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1005, nameof(DownloadFileContent)), "Download file content failed because file was not found. FilePath={FilePath}");
    private static readonly Action<ILogger, string, Exception?> LogMoveSourceNotFoundMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1006, nameof(MoveFile)), "Move file failed because source file was not found. SourceFilePath={SourceFilePath}");
    private static readonly Action<ILogger, string, Exception?> LogMoveTargetNotFoundMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1007, nameof(MoveFile)), "Move file failed because target directory was not found. TargetDirectoryPath={TargetDirectoryPath}");
    private static readonly Action<ILogger, string, Exception?> LogRenameSourceNotFoundMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1008, nameof(RenameFile)), "Rename file failed because source file was not found. FilePath={FilePath}");
    private static readonly Action<ILogger, string, Exception?> LogDeleteSourceNotFoundMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1009, nameof(DeleteFile)), "Delete file failed because source file was not found. FilePath={FilePath}");

    private readonly CloudDirectory _root;
    private readonly CloudFileFactoryRegistry _factoryRegistry;
    private readonly IStorageMetadataGateway _storageMetadataGateway;
    private readonly AppConfig _config;
    private readonly ExtensionPolicy _extensionPolicy;
    private readonly ILogger<CloudFileFileCommandService> _logger;

    /// <summary>
    /// 初始化 CloudFileFileCommandService。
    /// </summary>
    public CloudFileFileCommandService(
        CloudDirectory root,
        CloudFileFactoryRegistry factoryRegistry,
        IStorageMetadataGateway storageMetadataGateway,
        AppConfig config,
        ILogger<CloudFileFileCommandService>? logger = null)
    {
        _root = root;
        _factoryRegistry = factoryRegistry;
        _storageMetadataGateway = storageMetadataGateway;
        _config = config;
        _extensionPolicy = new ExtensionPolicy(config);
        _logger = logger ?? NullLogger<CloudFileFileCommandService>.Instance;
    }

    /// <summary>
    /// 上傳檔案。
    /// </summary>
    public OperationResult UploadFile(UploadFileRequest request)
    {
        LogUploadRequestedMessage(_logger, request.DirectoryPath, request.FileName, null);
        OperationResult? prepareResult = TryPrepareUpload(request, out UploadPreparation? preparation);
        if (prepareResult is not null)
        {
            LogUploadValidationFailedMessage(_logger, request.DirectoryPath, request.FileName, prepareResult.Message, null);
            return prepareResult;
        }

        if (preparation is null)
        {
            return new OperationResult(false, "Upload preparation failed.", OperationErrorCodes.UnexpectedError);
        }

        try
        {
            CloudFile file = _factoryRegistry.Create(preparation.Request, DateTime.UtcNow);
            OperationResult persistenceResult = PersistUploadIfNeeded(preparation.Request, file.FileType);
            if (!persistenceResult.Success)
            {
                LogUploadPersistenceFailedMessage(_logger, preparation.Request.DirectoryPath, preparation.Request.FileName, persistenceResult.ErrorCode, null);
                return persistenceResult;
            }

            CloudFile? replacedFile = preparation.ReplaceExistingFile
                ? DetachExistingFile(preparation.Directory, preparation.Request.FileName)
                : null;

            if (preparation.ReplaceExistingFile && replacedFile is null)
            {
                return new OperationResult(false, $"Unable to overwrite existing file: {preparation.Request.FileName}");
            }

            try
            {
                preparation.Directory.AddFile(file);
            }
            catch
            {
                if (replacedFile is not null)
                {
                    preparation.Directory.AddFile(replacedFile);
                }

                throw;
            }

            return new OperationResult(true, "File uploaded.");
        }
        catch (InvalidOperationException ex) { return new OperationResult(false, ex.Message); }
        catch (ArgumentException ex) { return new OperationResult(false, ex.Message); }
        catch { return new OperationResult(false, "Upload failed due to an unexpected error.", OperationErrorCodes.UnexpectedError); }
    }

    public async Task<OperationResult> UploadFileAsync(UploadFileRequest request, CancellationToken cancellationToken = default)
    {
        LogUploadRequestedMessage(_logger, request.DirectoryPath, request.FileName, null);
        OperationResult? prepareResult = TryPrepareUpload(request, out UploadPreparation? preparation);
        if (prepareResult is not null)
        {
            LogUploadValidationFailedMessage(_logger, request.DirectoryPath, request.FileName, prepareResult.Message, null);
            return prepareResult;
        }

        if (preparation is null)
        {
            return new OperationResult(false, "Upload preparation failed.", OperationErrorCodes.UnexpectedError);
        }

        try
        {
            CloudFile file = _factoryRegistry.Create(preparation.Request, DateTime.UtcNow);
            OperationResult persistenceResult = await PersistUploadIfNeededAsync(preparation.Request, file.FileType, cancellationToken);
            if (!persistenceResult.Success)
            {
                LogUploadPersistenceFailedMessage(_logger, preparation.Request.DirectoryPath, preparation.Request.FileName, persistenceResult.ErrorCode, null);
                return persistenceResult;
            }

            CloudFile? replacedFile = preparation.ReplaceExistingFile
                ? DetachExistingFile(preparation.Directory, preparation.Request.FileName)
                : null;

            if (preparation.ReplaceExistingFile && replacedFile is null)
            {
                return new OperationResult(false, $"Unable to overwrite existing file: {preparation.Request.FileName}");
            }

            try
            {
                preparation.Directory.AddFile(file);
            }
            catch
            {
                if (replacedFile is not null)
                {
                    preparation.Directory.AddFile(replacedFile);
                }

                throw;
            }

            return new OperationResult(true, "File uploaded.");
        }
        catch (InvalidOperationException ex) { return new OperationResult(false, ex.Message); }
        catch (ArgumentException ex) { return new OperationResult(false, ex.Message); }
        catch { return new OperationResult(false, "Upload failed due to an unexpected error.", OperationErrorCodes.UnexpectedError); }
    }

    private OperationResult? TryPrepareUpload(UploadFileRequest request, out UploadPreparation? preparation)
    {
        preparation = null;

        string? fileNameError = NodeNameValidator.Validate(request.FileName, "File");
        if (fileNameError is not null)
        {
            return new OperationResult(false, fileNameError);
        }

        string extension = Path.GetExtension(request.FileName);
        if (!_extensionPolicy.IsAllowedAny(extension))
        {
            return new OperationResult(false, $"Unsupported file extension: {extension}");
        }

        if (request.Size > _config.Management.MaxUploadSizeBytes)
        {
            return new OperationResult(false, $"Upload size exceeds limit: {_config.Management.MaxUploadSizeBytes} bytes");
        }

        CloudDirectory? directory = CloudFileTreeLookup.FindDirectory(_root, request.DirectoryPath);
        if (directory is null)
        {
            return new OperationResult(false, $"Directory not found: {request.DirectoryPath}");
        }

        UploadFileRequest normalizedRequest = request;
        bool replaceExistingFile = false;

        bool duplicated = directory.Files.Any(item => item.Name.Equals(request.FileName, StringComparison.OrdinalIgnoreCase));
        if (duplicated)
        {
            switch (_config.Management.GetFileConflictPolicy())
            {
                case FileConflictPolicyType.Reject:
                    return new OperationResult(false, $"File already exists: {request.FileName}");
                case FileConflictPolicyType.Overwrite:
                    replaceExistingFile = true;
                    break;
                case FileConflictPolicyType.Rename:
                    string renamed = UniqueFileNameResolver.Resolve(
                        request.FileName,
                        candidate => directory.Files.Any(item => item.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase)));
                    normalizedRequest = new UploadFileRequest(
                        request.DirectoryPath,
                        renamed,
                        request.Size,
                        request.PageCount,
                        request.Width,
                        request.Height,
                        request.Encoding,
                        request.SourceLocalPath);
                    break;
                default:
                    return new OperationResult(false, $"Unsupported file conflict policy: {_config.Management.FileConflictPolicy}");
            }
        }

        preparation = new UploadPreparation(directory, normalizedRequest, replaceExistingFile);
        return null;
    }

    private OperationResult PersistUploadIfNeeded(UploadFileRequest request, CloudFileType fileType)
    {
        return _storageMetadataGateway.UploadFile(request, fileType);
    }

    private Task<OperationResult> PersistUploadIfNeededAsync(UploadFileRequest request, CloudFileType fileType, CancellationToken cancellationToken)
    {
        return _storageMetadataGateway.UploadFileAsync(request, fileType, cancellationToken);
    }

    private static CloudFile? DetachExistingFile(CloudDirectory directory, string fileName)
    {
        CloudFile? existingFile = directory.Files
            .FirstOrDefault(item => item.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

        if (existingFile is null)
        {
            return null;
        }

        return directory.RemoveFile(existingFile.Name)
            ? existingFile
            : null;
    }

    private sealed record UploadPreparation(CloudDirectory Directory, UploadFileRequest Request, bool ReplaceExistingFile);

    /// <summary>
    /// 下載檔案。
    /// </summary>
    public OperationResult DownloadFile(DownloadFileRequest request)
    {
        var sourceResult = CloudFileTreeLookup.FindFileWithParent(_root, request.FilePath);
        if (sourceResult.File is null)
        {
            LogDownloadFileNotFoundMessage(_logger, request.FilePath, null);
            return new OperationResult(false, $"File not found: {request.FilePath}");
        }

        return _storageMetadataGateway.DownloadFile(request.FilePath, request.TargetLocalPath);
    }

    public Task<OperationResult> DownloadFileAsync(DownloadFileRequest request, CancellationToken cancellationToken = default)
    {
        var sourceResult = CloudFileTreeLookup.FindFileWithParent(_root, request.FilePath);
        if (sourceResult.File is null)
        {
            return Task.FromResult(new OperationResult(false, $"File not found: {request.FilePath}"));
        }

        return _storageMetadataGateway.DownloadFileAsync(request.FilePath, request.TargetLocalPath, cancellationToken);
    }

    /// <summary>
    /// 下載檔案內容。
    /// </summary>
    public FileDownloadResult DownloadFileContent(string filePath)
    {
        var sourceResult = CloudFileTreeLookup.FindFileWithParent(_root, filePath);
        if (sourceResult.File is null)
        {
            LogDownloadContentNotFoundMessage(_logger, filePath, null);
            return new FileDownloadResult(false, $"File not found: {filePath}", string.Empty, null, "application/octet-stream");
        }

        return _storageMetadataGateway.DownloadFileContent(filePath);
    }

    public Task<FileDownloadResult> DownloadFileContentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var sourceResult = CloudFileTreeLookup.FindFileWithParent(_root, filePath);
        if (sourceResult.File is null)
        {
            return Task.FromResult(new FileDownloadResult(false, $"File not found: {filePath}", string.Empty, null, "application/octet-stream"));
        }

        return _storageMetadataGateway.DownloadFileContentAsync(filePath, cancellationToken);
    }

    /// <summary>
    /// 搬移檔案。
    /// </summary>
    public OperationResult MoveFile(MoveFileRequest request)
    {
        var sourceResult = CloudFileTreeLookup.FindFileWithParent(_root, request.SourceFilePath);
        if (sourceResult.ParentDirectory is null || sourceResult.File is null)
        {
            LogMoveSourceNotFoundMessage(_logger, request.SourceFilePath, null);
            return new OperationResult(false, $"File not found: {request.SourceFilePath}");
        }

        CloudDirectory? targetDirectory = CloudFileTreeLookup.FindDirectory(_root, request.TargetDirectoryPath);
        if (targetDirectory is null)
        {
            LogMoveTargetNotFoundMessage(_logger, request.TargetDirectoryPath, null);
            return new OperationResult(false, $"Target directory not found: {request.TargetDirectoryPath}");
        }

        {
            OperationResult persistenceResult = _storageMetadataGateway.MoveFile(request.SourceFilePath, request.TargetDirectoryPath);
            if (!persistenceResult.Success)
            {
                return persistenceResult;
            }
        }

        if (!sourceResult.ParentDirectory.RemoveFile(sourceResult.File.Name))
        {
            return new OperationResult(false, "Unable to remove source file from memory tree.");
        }

        try
        {
            targetDirectory.AddFile(sourceResult.File);
        }
        catch (InvalidOperationException ex)
        {
            sourceResult.ParentDirectory.AddFile(sourceResult.File);
            return new OperationResult(false, ex.Message);
        }
        catch
        {
            sourceResult.ParentDirectory.AddFile(sourceResult.File);
            return new OperationResult(false, "Move file failed due to an unexpected error.", OperationErrorCodes.MoveFileUnexpected);
        }

        return new OperationResult(true, "File moved.");
    }

    public async Task<OperationResult> MoveFileAsync(MoveFileRequest request, CancellationToken cancellationToken = default)
    {
        var sourceResult = CloudFileTreeLookup.FindFileWithParent(_root, request.SourceFilePath);
        if (sourceResult.ParentDirectory is null || sourceResult.File is null)
        {
            LogMoveSourceNotFoundMessage(_logger, request.SourceFilePath, null);
            return new OperationResult(false, $"File not found: {request.SourceFilePath}");
        }

        CloudDirectory? targetDirectory = CloudFileTreeLookup.FindDirectory(_root, request.TargetDirectoryPath);
        if (targetDirectory is null)
        {
            LogMoveTargetNotFoundMessage(_logger, request.TargetDirectoryPath, null);
            return new OperationResult(false, $"Target directory not found: {request.TargetDirectoryPath}");
        }

        OperationResult persistenceResult = await _storageMetadataGateway.MoveFileAsync(request.SourceFilePath, request.TargetDirectoryPath, cancellationToken);
        if (!persistenceResult.Success)
        {
            return persistenceResult;
        }

        if (!sourceResult.ParentDirectory.RemoveFile(sourceResult.File.Name))
        {
            return new OperationResult(false, "Unable to remove source file from memory tree.");
        }

        try
        {
            targetDirectory.AddFile(sourceResult.File);
        }
        catch (InvalidOperationException ex)
        {
            sourceResult.ParentDirectory.AddFile(sourceResult.File);
            return new OperationResult(false, ex.Message);
        }
        catch
        {
            sourceResult.ParentDirectory.AddFile(sourceResult.File);
            return new OperationResult(false, "Move file failed due to an unexpected error.", OperationErrorCodes.MoveFileUnexpected);
        }

        return new OperationResult(true, "File moved.");
    }

    /// <summary>
    /// 重新命名檔案。
    /// </summary>
    public OperationResult RenameFile(RenameFileRequest request)
    {
        string? fileNameError = NodeNameValidator.Validate(request.NewFileName, "File");
        if (fileNameError is not null)
        {
            return new OperationResult(false, fileNameError);
        }

        var sourceResult = CloudFileTreeLookup.FindFileWithParent(_root, request.FilePath);
        if (sourceResult.ParentDirectory is null || sourceResult.File is null)
        {
            LogRenameSourceNotFoundMessage(_logger, request.FilePath, null);
            return new OperationResult(false, $"File not found: {request.FilePath}");
        }

        {
            OperationResult persistenceResult = _storageMetadataGateway.RenameFile(request.FilePath, request.NewFileName);
            if (!persistenceResult.Success)
            {
                return persistenceResult;
            }
        }

        try
        {
            sourceResult.File.Rename(request.NewFileName);
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
            return new OperationResult(false, "Rename file failed due to an unexpected error.", OperationErrorCodes.RenameFileUnexpected);
        }

        return new OperationResult(true, "File renamed.");
    }

    public async Task<OperationResult> RenameFileAsync(RenameFileRequest request, CancellationToken cancellationToken = default)
    {
        string? fileNameError = NodeNameValidator.Validate(request.NewFileName, "File");
        if (fileNameError is not null)
        {
            return new OperationResult(false, fileNameError);
        }

        var sourceResult = CloudFileTreeLookup.FindFileWithParent(_root, request.FilePath);
        if (sourceResult.ParentDirectory is null || sourceResult.File is null)
        {
            LogRenameSourceNotFoundMessage(_logger, request.FilePath, null);
            return new OperationResult(false, $"File not found: {request.FilePath}");
        }

        OperationResult persistenceResult = await _storageMetadataGateway.RenameFileAsync(request.FilePath, request.NewFileName, cancellationToken);
        if (!persistenceResult.Success)
        {
            return persistenceResult;
        }

        try
        {
            sourceResult.File.Rename(request.NewFileName);
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
            return new OperationResult(false, "Rename file failed due to an unexpected error.", OperationErrorCodes.RenameFileUnexpected);
        }

        return new OperationResult(true, "File renamed.");
    }

    /// <summary>
    /// 刪除檔案。
    /// </summary>
    public OperationResult DeleteFile(DeleteFileRequest request)
    {
        var sourceResult = CloudFileTreeLookup.FindFileWithParent(_root, request.FilePath);
        if (sourceResult.ParentDirectory is null || sourceResult.File is null)
        {
            LogDeleteSourceNotFoundMessage(_logger, request.FilePath, null);
            return new OperationResult(false, $"File not found: {request.FilePath}");
        }

        {
            OperationResult persistenceResult = _storageMetadataGateway.DeleteFile(request.FilePath);
            if (!persistenceResult.Success)
            {
                return persistenceResult;
            }
        }

        bool removed = sourceResult.ParentDirectory.RemoveFile(sourceResult.File.Name);
        return removed
            ? new OperationResult(true, "File deleted.")
            : new OperationResult(false, "Unable to remove file from memory tree.");
    }

    public async Task<OperationResult> DeleteFileAsync(DeleteFileRequest request, CancellationToken cancellationToken = default)
    {
        var sourceResult = CloudFileTreeLookup.FindFileWithParent(_root, request.FilePath);
        if (sourceResult.ParentDirectory is null || sourceResult.File is null)
        {
            LogDeleteSourceNotFoundMessage(_logger, request.FilePath, null);
            return new OperationResult(false, $"File not found: {request.FilePath}");
        }

        OperationResult persistenceResult = await _storageMetadataGateway.DeleteFileAsync(request.FilePath, cancellationToken);
        if (!persistenceResult.Success)
        {
            return persistenceResult;
        }

        bool removed = sourceResult.ParentDirectory.RemoveFile(sourceResult.File.Name);
        return removed
            ? new OperationResult(true, "File deleted.")
            : new OperationResult(false, "Unable to remove file from memory tree.");
    }
}
