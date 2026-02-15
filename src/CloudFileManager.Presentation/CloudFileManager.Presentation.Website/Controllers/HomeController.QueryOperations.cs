using CloudFileManager.Contracts;
using CloudFileManager.Presentation.Website.Models;
using Microsoft.AspNetCore.Mvc;

namespace CloudFileManager.Presentation.Website.Controllers;

public partial class HomeController
{
    [HttpPost]
    public async Task<IActionResult> CalculateSize(string path, CancellationToken cancellationToken)
    {
        SizeCalculationResultDto result = await _apiClient.CalculateSizeAsync(path, cancellationToken);
        HomeIndexViewModel model = await BuildBaseModelAsync(cancellationToken);
        model.SizePath = path;

        if (!result.IsFound)
        {
            model.OperationSuccess = false;
            model.OperationMessage = $"Directory not found: {path}";
            return View("Index", model);
        }

        model.SizeFormatted = result.FormattedSize;
        model.SizeTraverseLog = result.TraverseLog;
        return View("Index", model);
    }

    [HttpPost]
    public async Task<IActionResult> Search(string extension, CancellationToken cancellationToken)
    {
        SearchResultDto result = await _apiClient.SearchAsync(extension, cancellationToken);
        HomeIndexViewModel model = await BuildBaseModelAsync(cancellationToken);
        model.SearchExtension = extension;
        model.SearchPaths = result.Paths;
        model.SearchTraverseLog = result.TraverseLog;
        return View("Index", model);
    }

    [HttpPost]
    public async Task<IActionResult> ExportXml(CancellationToken cancellationToken)
    {
        XmlExportResultDto result = await _apiClient.ExportXmlAsync(cancellationToken);
        HomeIndexViewModel model = await BuildBaseModelAsync(cancellationToken);
        model.XmlContent = result.XmlContent;
        model.XmlOutputPath = result.OutputPath;
        return View("Index", model);
    }

    [HttpPost]
    public async Task<IActionResult> LoadFeatureFlags(CancellationToken cancellationToken)
    {
        FeatureFlagsResultDto result = await _apiClient.GetFeatureFlagsAsync(cancellationToken);
        HomeIndexViewModel model = await BuildBaseModelAsync(cancellationToken);
        model.FeatureFlags = result.Flags;
        return View("Index", model);
    }
}
