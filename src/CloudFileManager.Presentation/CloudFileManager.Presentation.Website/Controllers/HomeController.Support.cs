using System.Diagnostics;
using CloudFileManager.Contracts;
using CloudFileManager.Presentation.Website.Models;
using CloudFileManager.Shared.Common;
using Microsoft.AspNetCore.Mvc;

namespace CloudFileManager.Presentation.Website.Controllers;

public partial class HomeController
{
    private async Task<HomeIndexViewModel> BuildBaseModelAsync(CancellationToken cancellationToken, OperationResultDto? operationResult = null)
    {
        try
        {
            DirectoryTreeResultDto tree = await _apiClient.GetTreeAsync(cancellationToken);
            return new HomeIndexViewModel
            {
                TreeLines = tree.Lines,
                OperationSuccess = operationResult?.Success,
                OperationMessage = operationResult?.Message,
                OperationErrorCode = operationResult?.ErrorCode
            };
        }
        catch (HttpRequestException)
        {
            return new HomeIndexViewModel
            {
                ErrorMessage = "Unable to load directory tree due to network error.",
                ErrorCode = OperationErrorCodes.DirectoryTreeNetworkError,
                TreeLines = [],
                OperationSuccess = operationResult?.Success,
                OperationMessage = operationResult?.Message,
                OperationErrorCode = operationResult?.ErrorCode
            };
        }
        catch (TaskCanceledException)
        {
            return new HomeIndexViewModel
            {
                ErrorMessage = "Unable to load directory tree because the request timed out.",
                ErrorCode = OperationErrorCodes.DirectoryTreeTimeout,
                TreeLines = [],
                OperationSuccess = operationResult?.Success,
                OperationMessage = operationResult?.Message,
                OperationErrorCode = operationResult?.ErrorCode
            };
        }
        catch (InvalidOperationException ex)
        {
            return new HomeIndexViewModel
            {
                ErrorMessage = ex.Message,
                ErrorCode = OperationErrorCodes.DirectoryTreeUnexpected,
                TreeLines = [],
                OperationSuccess = operationResult?.Success,
                OperationMessage = operationResult?.Message,
                OperationErrorCode = operationResult?.ErrorCode
            };
        }
        catch
        {
            return new HomeIndexViewModel
            {
                ErrorMessage = "Unable to load directory tree due to an unexpected error.",
                ErrorCode = OperationErrorCodes.DirectoryTreeUnexpected,
                TreeLines = [],
                OperationSuccess = operationResult?.Success,
                OperationMessage = operationResult?.Message,
                OperationErrorCode = operationResult?.ErrorCode
            };
        }
    }

    private void WriteOperationResultToTempData(OperationResultDto result)
    {
        TempData[nameof(HomeIndexViewModel.OperationSuccess)] = result.Success;
        TempData[nameof(HomeIndexViewModel.OperationMessage)] = result.Message;
        TempData[nameof(HomeIndexViewModel.OperationErrorCode)] = result.ErrorCode;
    }

    private RedirectToActionResult RedirectToIndexWithResult(OperationResultDto result)
    {
        WriteOperationResultToTempData(result);
        return RedirectToAction(nameof(Index));
    }

    private OperationResultDto? ReadOperationResultFromTempData()
    {
        if (!TempData.TryGetValue(nameof(HomeIndexViewModel.OperationSuccess), out object? successRaw) ||
            !TempData.TryGetValue(nameof(HomeIndexViewModel.OperationMessage), out object? messageRaw))
        {
            return null;
        }

        _ = TempData.TryGetValue(nameof(HomeIndexViewModel.OperationErrorCode), out object? errorCodeRaw);

        bool success = successRaw switch
        {
            bool flag => flag,
            string text when bool.TryParse(text, out bool parsed) => parsed,
            _ => false
        };

        if (messageRaw is not string message)
        {
            return null;
        }

        string? errorCode = errorCodeRaw as string;
        return new OperationResultDto(success, message, errorCode);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
