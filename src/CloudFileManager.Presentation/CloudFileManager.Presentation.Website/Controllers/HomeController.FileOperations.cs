using CloudFileManager.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CloudFileManager.Presentation.Website.Controllers;

public partial class HomeController
{
    [HttpPost]
    public async Task<IActionResult> UploadFile(string directoryPath, IFormFile file, CancellationToken cancellationToken)
    {
        OperationResultDto result = await _apiClient.UploadFileAsync(directoryPath, file, cancellationToken);
        return RedirectToIndexWithResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> MoveFile(string sourceFilePath, string targetDirectoryPath, CancellationToken cancellationToken)
    {
        OperationResultDto result = await _apiClient.MoveFileAsync(new MoveFileRequestDto(sourceFilePath, targetDirectoryPath), cancellationToken);
        return RedirectToIndexWithResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> RenameFile(string filePath, string newFileName, CancellationToken cancellationToken)
    {
        OperationResultDto result = await _apiClient.RenameFileAsync(new RenameFileRequestDto(filePath, newFileName), cancellationToken);
        return RedirectToIndexWithResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteFile(string filePath, CancellationToken cancellationToken)
    {
        OperationResultDto result = await _apiClient.DeleteFileAsync(new DeleteFileRequestDto(filePath), cancellationToken);
        return RedirectToIndexWithResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> DownloadFile(string filePath, CancellationToken cancellationToken)
    {
        FileDownloadResultDto result = await _apiClient.DownloadFileContentAsync(filePath, cancellationToken);
        if (!result.Success || result.Content is null)
        {
            return View("Index", await BuildBaseModelAsync(cancellationToken, new OperationResultDto(false, result.Message)));
        }

        return File(result.Content, result.ContentType, result.FileName);
    }
}
