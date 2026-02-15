using System.Diagnostics;
using CloudFileManager.Contracts;
using Microsoft.AspNetCore.Mvc;
using CloudFileManager.Presentation.Website.Models;
using CloudFileManager.Presentation.Website.Services;

namespace CloudFileManager.Presentation.Website.Controllers;

/// <summary>
/// HomeController 類別，負責處理 HTTP 請求與回應。
/// </summary>
public partial class HomeController : Controller
{
    private readonly FileSystemApiClient _apiClient;

    /// <summary>
    /// 初始化 HomeController。
    /// </summary>
    public HomeController(FileSystemApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// 顯示首頁。
    /// </summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        HomeIndexViewModel model = await BuildBaseModelAsync(cancellationToken, ReadOperationResultFromTempData());
        return View(model);
    }
}
