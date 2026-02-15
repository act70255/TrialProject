using CloudFileManager.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CloudFileManager.Presentation.Website.Controllers;

public partial class HomeController
{
    [HttpPost]
    public async Task<IActionResult> CreateDirectory(string parentPath, string directoryName, CancellationToken cancellationToken)
    {
        OperationResultDto result = await _apiClient.CreateDirectoryAsync(new CreateDirectoryRequestDto(parentPath, directoryName), cancellationToken);
        return RedirectToIndexWithResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> MoveDirectory(string sourceDirectoryPath, string targetParentDirectoryPath, CancellationToken cancellationToken)
    {
        OperationResultDto result = await _apiClient.MoveDirectoryAsync(new MoveDirectoryRequestDto(sourceDirectoryPath, targetParentDirectoryPath), cancellationToken);
        return RedirectToIndexWithResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> RenameDirectory(string directoryPath, string newDirectoryName, CancellationToken cancellationToken)
    {
        OperationResultDto result = await _apiClient.RenameDirectoryAsync(new RenameDirectoryRequestDto(directoryPath, newDirectoryName), cancellationToken);
        return RedirectToIndexWithResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteDirectory(string directoryPath, CancellationToken cancellationToken)
    {
        OperationResultDto result = await _apiClient.DeleteDirectoryAsync(new DeleteDirectoryRequestDto(directoryPath), cancellationToken);
        return RedirectToIndexWithResult(result);
    }
}
