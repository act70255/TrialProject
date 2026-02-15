using CloudFileManager.Application.Models;
using CloudFileManager.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CloudFileManager.Infrastructure.FileStorage;

public sealed partial class StorageMetadataGateway
{
    private static readonly Action<ILogger, Exception?> LogDatabaseSaveFailedMessage =
        LoggerMessage.Define(LogLevel.Error, new EventId(1401, "DatabaseSaveFailed"), "Database save failed during storage persistence.");
    private static readonly Action<ILogger, Exception?> LogInvalidOperationSaveFailedMessage =
        LoggerMessage.Define(LogLevel.Error, new EventId(1402, "InvalidOperationSaveFailed"), "Invalid operation during storage persistence save.");
    private static readonly Action<ILogger, Exception?> LogUnexpectedSaveFailedMessage =
        LoggerMessage.Define(LogLevel.Error, new EventId(1403, "UnexpectedSaveFailed"), "Unexpected error during storage persistence save.");

    private OperationResult ExecuteSaveWithRollback(
        Action restoreEntityState,
        Func<bool> rollbackPhysicalState,
        string successAuditEntry,
        string failureAuditPrefix,
        string successMessage,
        string rollbackFailureMessage,
        string rollbackFailureErrorCode)
    {
        try
        {
            _dbContext.SaveChanges();
            _auditTrailWriter.Write(successAuditEntry);
            return new OperationResult(true, successMessage);
        }
        catch (DbUpdateException ex)
        {
            LogDatabaseSaveFailedMessage(_logger, ex);
            return HandleSaveFailure(
                restoreEntityState,
                rollbackPhysicalState,
                failureAuditPrefix,
                ex.Message,
                rollbackFailureMessage,
                rollbackFailureErrorCode);
        }
        catch (InvalidOperationException ex)
        {
            LogInvalidOperationSaveFailedMessage(_logger, ex);
            return HandleSaveFailure(
                restoreEntityState,
                rollbackPhysicalState,
                failureAuditPrefix,
                ex.Message,
                rollbackFailureMessage,
                rollbackFailureErrorCode);
        }
        catch
        {
            LogUnexpectedSaveFailedMessage(_logger, null);
            return HandleSaveFailure(
                restoreEntityState,
                rollbackPhysicalState,
                failureAuditPrefix,
                "UNEXPECTED",
                rollbackFailureMessage,
                rollbackFailureErrorCode);
        }
    }

    private async Task<OperationResult> ExecuteSaveWithRollbackAsync(
        Action restoreEntityState,
        Func<bool> rollbackPhysicalState,
        string successAuditEntry,
        string failureAuditPrefix,
        string successMessage,
        string rollbackFailureMessage,
        string rollbackFailureErrorCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _auditTrailWriter.Write(successAuditEntry);
            return new OperationResult(true, successMessage);
        }
        catch (DbUpdateException ex)
        {
            LogDatabaseSaveFailedMessage(_logger, ex);
            return HandleSaveFailure(
                restoreEntityState,
                rollbackPhysicalState,
                failureAuditPrefix,
                ex.Message,
                rollbackFailureMessage,
                rollbackFailureErrorCode);
        }
        catch (InvalidOperationException ex)
        {
            LogInvalidOperationSaveFailedMessage(_logger, ex);
            return HandleSaveFailure(
                restoreEntityState,
                rollbackPhysicalState,
                failureAuditPrefix,
                ex.Message,
                rollbackFailureMessage,
                rollbackFailureErrorCode);
        }
        catch
        {
            LogUnexpectedSaveFailedMessage(_logger, null);
            return HandleSaveFailure(
                restoreEntityState,
                rollbackPhysicalState,
                failureAuditPrefix,
                "UNEXPECTED",
                rollbackFailureMessage,
                rollbackFailureErrorCode);
        }
    }

    private OperationResult HandleSaveFailure(
        Action restoreEntityState,
        Func<bool> rollbackPhysicalState,
        string failureAuditPrefix,
        string failureDetail,
        string rollbackFailureMessage,
        string rollbackFailureErrorCode)
    {
        restoreEntityState();
        bool rollbackSucceeded = rollbackPhysicalState();
        _auditTrailWriter.Write($"{failureAuditPrefix}|{failureDetail}");
        if (rollbackSucceeded)
        {
            return new OperationResult(false, rollbackFailureMessage, rollbackFailureErrorCode);
        }

        _auditTrailWriter.Write($"{failureAuditPrefix}|ROLLBACK_FAILED");
        return new OperationResult(
            false,
            "Database update failed after physical operation, and rollback also failed. Manual intervention is required.",
            OperationErrorCodes.PersistenceRollbackFailed);
    }
}
