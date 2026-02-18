using CloudFileManager.Application.Interfaces;
using CloudFileManager.Application.Models;
using CloudFileManager.Presentation.WebApi.Model;
using CloudFileManager.Presentation.WebApi.Services;
using CloudFileManager.Shared.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CloudFileManager.Presentation.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/filesystem")]
/// <summary>
/// FileSystemController 類別，負責處理 HTTP 請求與回應。
/// </summary>
public sealed partial class FileSystemController : ControllerBase
{
    private static readonly Action<ILogger, string, string, bool, string?, Exception?> LogOperationCompletedMessage =
        LoggerMessage.Define<string, string, bool, string?>(LogLevel.Information, new EventId(1201, "OperationCompleted"), "Operation completed. Method={Method}, Path={Path}, Success={Success}, ErrorCode={ErrorCode}");
    private static readonly Action<ILogger, string, string, Exception?> LogFileContentDownloadFailedMessage =
        LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(1202, "FileContentDownloadFailed"), "File content download failed. Path={Path}, Message={Message}");

    private readonly ICloudFileApplicationService _service;
    private readonly ILocalFileUploadRequestFactory _localFileUploadRequestFactory;
    private readonly WebApiSessionStateProcessor _sessionStateProcessor;
    private readonly ILogger<FileSystemController> _logger;

    /// <summary>
    /// 初始化 FileSystemController。
    /// </summary>
    public FileSystemController(
        ICloudFileApplicationService service,
        ILocalFileUploadRequestFactory localFileUploadRequestFactory,
        WebApiSessionStateProcessor sessionStateProcessor,
        ILogger<FileSystemController> logger)
    {
        _service = service;
        _localFileUploadRequestFactory = localFileUploadRequestFactory;
        _sessionStateProcessor = sessionStateProcessor;
        _logger = logger;
    }

    [HttpGet("tree")]
    /// <summary>
    /// 取得樹狀結構。
    /// </summary>
    public ActionResult<DirectoryTreeApiResponse> GetTree()
    {
        return Ok(_service.GetDirectoryTree().ToApi());
    }

    [HttpGet("size")]
    /// <summary>
    /// 計算容量。
    /// </summary>
    public ActionResult<SizeCalculationApiResponse> CalculateSize([FromQuery] CalculateSizeApiRequest request)
    {
        return Ok(_service.CalculateTotalSize(request.ToApplication()).ToApi());
    }

    [HttpGet("search")]
    /// <summary>
    /// 搜尋資料。
    /// </summary>
    public ActionResult<SearchApiResponse> Search([FromQuery] SearchByExtensionApiRequest request)
    {
        return Ok(_service.SearchByExtension(request.ToApplication()).ToApi());
    }

    [HttpGet("xml")]
    /// <summary>
    /// 匯出目錄樹 XML。
    /// </summary>
    public ActionResult<XmlExportApiResponse> ExportXml([FromQuery] ExportXmlApiRequest request)
    {
        return Ok(_service.ExportXml(request.ToApplication()).ToApi());
    }

    [HttpGet("feature-flags")]
    /// <summary>
    /// 取得功能旗標。
    /// </summary>
    public ActionResult<FeatureFlagsApiResponse> GetFeatureFlags()
    {
        return Ok(_service.GetFeatureFlags().ToApi());
    }

    private ActionResult<OperationApiResponse> ToOperationActionResult(OperationResult result)
    {
        LogOperationCompletedMessage(
            _logger,
            HttpContext.Request.Method,
            HttpContext.Request.Path,
            result.Success,
            result.ErrorCode,
            null);

        OperationApiResponse response = result.ToApi();
        if (response.Success)
        {
            return Ok(response);
        }

        int statusCode = GetStatusCode(result);
        return StatusCode(statusCode, response);
    }

    private ActionResult<StatefulApiResponse<TData>> ToStatefulActionResult<TData>(SessionCommandResult<TData> result)
    {
        LogOperationCompletedMessage(
            _logger,
            HttpContext.Request.Method,
            HttpContext.Request.Path,
            result.Operation.Success,
            result.Operation.ErrorCode,
            null);

        StatefulApiResponse<TData> response = new(
            result.Operation.Success,
            result.Operation.Message,
            result.Operation.ErrorCode,
            result.Data,
            result.State,
            result.OutputLines);
        if (response.Success)
        {
            return Ok(response);
        }

        int statusCode = GetStatusCode(result.Operation);
        return StatusCode(statusCode, response);
    }

    private ObjectResult BadOperation(string message, string? errorCode = null)
    {
        int statusCode = GetStatusCode(new OperationResult(false, message, errorCode));
        return StatusCode(statusCode, new OperationApiResponse(false, message, errorCode));
    }

    private static int GetStatusCode(OperationResult result)
    {
        return result.ErrorCode switch
        {
            OperationErrorCodes.ResourceNotFound => StatusCodes.Status404NotFound,
            OperationErrorCodes.NameConflict => StatusCodes.Status409Conflict,
            OperationErrorCodes.ValidationFailed or OperationErrorCodes.UploadInvalidRequest => StatusCodes.Status400BadRequest,
            OperationErrorCodes.PolicyViolation => StatusCodes.Status422UnprocessableEntity,
            OperationErrorCodes.UploadPermissionDenied => StatusCodes.Status403Forbidden,
            OperationErrorCodes.PersistenceRollbackFailed or
            OperationErrorCodes.CopyDirectoryRollbackFailed or
            OperationErrorCodes.UnexpectedError or
            OperationErrorCodes.CopyFileUnexpected or
            OperationErrorCodes.CopyDirectoryUnexpected or
            OperationErrorCodes.UploadMetadataSaveFailed or
            OperationErrorCodes.DeleteFileUnexpected or
            OperationErrorCodes.MoveFileUnexpected or
            OperationErrorCodes.RenameFileUnexpected or
            OperationErrorCodes.MoveDirectoryUnexpected or
            OperationErrorCodes.RenameDirectoryUnexpected or
            OperationErrorCodes.CreateDirectoryUnexpected or
            OperationErrorCodes.DeleteDirectoryUnexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static bool IsMissingUploadFile(UploadFileFormApiRequest request)
    {
        return request.File is null || request.File.Length == 0;
    }

    private UploadFileRequest BuildUploadRequest(string directoryPath, string fileName, string temporaryFilePath)
    {
        return _localFileUploadRequestFactory.Create(directoryPath, fileName, temporaryFilePath);
    }

    private IActionResult ToFileContentActionResult(FileDownloadResult result)
    {
        if (!result.Success || result.Content is null)
        {
            LogFileContentDownloadFailedMessage(_logger, HttpContext.Request.Path, result.Message, null);
            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new OperationApiResponse(false, result.Message, OperationErrorCodes.ResourceNotFound));
            }

            return BadOperation(result.Message);
        }

        return File(result.Content, result.ContentType, result.FileName);
    }
}
