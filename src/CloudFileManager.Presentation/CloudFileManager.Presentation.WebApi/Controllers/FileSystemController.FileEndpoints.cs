using CloudFileManager.Application.Models;
using CloudFileManager.Presentation.WebApi.Model;
using CloudFileManager.Shared.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CloudFileManager.Presentation.WebApi.Controllers;

public sealed partial class FileSystemController
{
    [HttpPost("files")]
    /// <summary>
    /// 上傳檔案。
    /// </summary>
    public async Task<ActionResult<OperationApiResponse>> UploadFile([FromBody] UploadFileApiRequest request, CancellationToken cancellationToken)
    {
        OperationResult result = await _service.UploadFileAsync(request.ToApplication(), cancellationToken);
        return ToOperationActionResult(result);
    }

    [HttpPost("files/upload-form")]
    /// <summary>
    /// 透過表單上傳檔案。
    /// </summary>
    public async Task<ActionResult<OperationApiResponse>> UploadFileFromForm([FromForm] UploadFileFormApiRequest request, CancellationToken cancellationToken)
    {
        if (IsMissingUploadFile(request))
        {
            return BadOperation("No file uploaded.", OperationErrorCodes.UploadInvalidRequest);
        }

        IFormFile formFile = request.File ?? throw new InvalidOperationException("Upload file is required.");

        try
        {
            using TemporaryUploadFileScope tempFile = await TemporaryUploadFileScope.CreateAsync(formFile, cancellationToken);
            UploadFileRequest uploadRequest = BuildUploadRequest(request.DirectoryPath, formFile.FileName, tempFile.FilePath);
            OperationResult result = await _service.UploadFileAsync(uploadRequest, cancellationToken);
            return ToOperationActionResult(result);
        }
        catch (IOException)
        {
            return BadOperation("File upload failed due to I/O error.", OperationErrorCodes.UploadIoError);
        }
        catch (UnauthorizedAccessException)
        {
            return BadOperation("File upload failed due to insufficient permissions.", OperationErrorCodes.UploadPermissionDenied);
        }
        catch (InvalidOperationException ex)
        {
            return BadOperation(ex.Message, OperationErrorCodes.UploadInvalidRequest);
        }
        catch
        {
            return BadOperation("File upload failed due to an unexpected error.", OperationErrorCodes.UnexpectedError);
        }
    }

    [HttpPost("files/move")]
    /// <summary>
    /// 搬移檔案。
    /// </summary>
    public async Task<ActionResult<OperationApiResponse>> MoveFile([FromBody] MoveFileApiRequest request, CancellationToken cancellationToken)
    {
        OperationResult result = await _service.MoveFileAsync(request.ToApplication(), cancellationToken);
        return ToOperationActionResult(result);
    }

    [HttpPost("files/rename")]
    /// <summary>
    /// 重新命名檔案。
    /// </summary>
    public async Task<ActionResult<OperationApiResponse>> RenameFile([FromBody] RenameFileApiRequest request, CancellationToken cancellationToken)
    {
        OperationResult result = await _service.RenameFileAsync(request.ToApplication(), cancellationToken);
        return ToOperationActionResult(result);
    }

    [HttpDelete("files")]
    /// <summary>
    /// 刪除檔案。
    /// </summary>
    public async Task<ActionResult<OperationApiResponse>> DeleteFile([FromBody] DeleteFileApiRequest request, CancellationToken cancellationToken)
    {
        OperationResult result = await _service.DeleteFileAsync(request.ToApplication(), cancellationToken);
        return ToOperationActionResult(result);
    }

    [HttpGet("files/content")]
    /// <summary>
    /// 下載檔案內容。
    /// </summary>
    public async Task<IActionResult> DownloadFileContent([FromQuery][Required][MinLength(1)] string filePath, CancellationToken cancellationToken)
    {
        FileDownloadResult result = await _service.DownloadFileContentAsync(filePath, cancellationToken);
        return ToFileContentActionResult(result);
    }
}
