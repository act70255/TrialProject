namespace CloudFileManager.Shared.Common;

/// <summary>
/// 操作錯誤碼常數，供跨層錯誤判斷與追蹤使用。
/// </summary>
public static class OperationErrorCodes
{
    public const string ValidationFailed = "CFM_VALIDATION_FAILED";
    public const string ResourceNotFound = "CFM_RESOURCE_NOT_FOUND";
    public const string NameConflict = "CFM_NAME_CONFLICT";
    public const string PolicyViolation = "CFM_POLICY_VIOLATION";
    public const string UnexpectedError = "CFM_UNEXPECTED_ERROR";
    public const string CopyFileUnexpected = "CFM_COPY_FILE_UNEXPECTED";
    public const string CopyDirectoryUnexpected = "CFM_COPY_DIRECTORY_UNEXPECTED";
    public const string CopyDirectoryRollbackFailed = "CFM_COPY_DIRECTORY_ROLLBACK_FAILED";
    public const string UploadIoError = "CFM_UPLOAD_IO_ERROR";
    public const string UploadPermissionDenied = "CFM_UPLOAD_PERMISSION_DENIED";
    public const string UploadInvalidRequest = "CFM_UPLOAD_INVALID_REQUEST";
    public const string UploadMetadataSaveFailed = "CFM_UPLOAD_METADATA_SAVE_FAILED";
    public const string DeleteFileUnexpected = "CFM_DELETE_FILE_UNEXPECTED";
    public const string DeleteFileCleanupFailed = "CFM_DELETE_FILE_CLEANUP_FAILED";
    public const string MoveFileUnexpected = "CFM_MOVE_FILE_UNEXPECTED";
    public const string RenameFileUnexpected = "CFM_RENAME_FILE_UNEXPECTED";
    public const string MoveDirectoryUnexpected = "CFM_MOVE_DIRECTORY_UNEXPECTED";
    public const string RenameDirectoryUnexpected = "CFM_RENAME_DIRECTORY_UNEXPECTED";
    public const string CreateDirectoryUnexpected = "CFM_CREATE_DIRECTORY_UNEXPECTED";
    public const string DeleteDirectoryUnexpected = "CFM_DELETE_DIRECTORY_UNEXPECTED";
    public const string DeleteDirectoryCleanupFailed = "CFM_DELETE_DIRECTORY_CLEANUP_FAILED";
    public const string PersistenceRollbackFailed = "CFM_PERSISTENCE_ROLLBACK_FAILED";
    public const string DirectoryTreeNetworkError = "CFM_DIRECTORY_TREE_NETWORK_ERROR";
    public const string DirectoryTreeTimeout = "CFM_DIRECTORY_TREE_TIMEOUT";
    public const string DirectoryTreeUnexpected = "CFM_DIRECTORY_TREE_UNEXPECTED";
    public const string CommandExecutionUnexpected = "CFM_COMMAND_EXECUTION_UNEXPECTED";
}
