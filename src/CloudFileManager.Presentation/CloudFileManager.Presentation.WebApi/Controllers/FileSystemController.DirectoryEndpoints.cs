using CloudFileManager.Application.Models;
using CloudFileManager.Presentation.WebApi.Model;
using Microsoft.AspNetCore.Mvc;

namespace CloudFileManager.Presentation.WebApi.Controllers;

public sealed partial class FileSystemController
{
    [HttpPost("directories")]
    /// <summary>
    /// 建立目錄。
    /// </summary>
    public async Task<ActionResult<OperationApiResponse>> CreateDirectory([FromBody] CreateDirectoryApiRequest request, CancellationToken cancellationToken)
    {
        OperationResult result = await _service.CreateDirectoryAsync(request.ToApplication(), cancellationToken);
        return ToOperationActionResult(result);
    }

    [HttpDelete("directories")]
    /// <summary>
    /// 刪除目錄。
    /// </summary>
    public async Task<ActionResult<OperationApiResponse>> DeleteDirectory([FromBody] DeleteDirectoryApiRequest request, CancellationToken cancellationToken)
    {
        OperationResult result = await _service.DeleteDirectoryAsync(request.ToApplication(), cancellationToken);
        return ToOperationActionResult(result);
    }

    [HttpPost("directories/move")]
    /// <summary>
    /// 搬移目錄。
    /// </summary>
    public async Task<ActionResult<OperationApiResponse>> MoveDirectory([FromBody] MoveDirectoryApiRequest request, CancellationToken cancellationToken)
    {
        OperationResult result = await _service.MoveDirectoryAsync(request.ToApplication(), cancellationToken);
        return ToOperationActionResult(result);
    }

    [HttpPost("directories/rename")]
    /// <summary>
    /// 重新命名目錄。
    /// </summary>
    public async Task<ActionResult<OperationApiResponse>> RenameDirectory([FromBody] RenameDirectoryApiRequest request, CancellationToken cancellationToken)
    {
        OperationResult result = await _service.RenameDirectoryAsync(request.ToApplication(), cancellationToken);
        return ToOperationActionResult(result);
    }

    [HttpPost("directories/copy")]
    /// <summary>
    /// 複製目錄。
    /// </summary>
    public async Task<ActionResult<OperationApiResponse>> CopyDirectory([FromBody] CopyDirectoryApiRequest request, CancellationToken cancellationToken)
    {
        OperationResult result = await _service.CopyDirectoryAsync(request.ToApplication(), cancellationToken);
        return ToOperationActionResult(result);
    }

    [HttpPost("directories/change-current")]
    /// <summary>
    /// 切換目前目錄（由客戶端傳入 state）。
    /// </summary>
    public ActionResult<StatefulApiResponse<StateOperationInfoApiResponse>> ChangeCurrentDirectory([FromBody] ChangeCurrentDirectoryApiRequest request)
    {
        SessionCommandResult<StateOperationInfoApiResponse> result = _sessionStateProcessor.ChangeCurrentDirectory(request);
        return ToStatefulActionResult(result);
    }

    [HttpPost("directories/sort")]
    /// <summary>
    /// 設定遞迴清單排序規則（由客戶端傳入 state）。
    /// </summary>
    public ActionResult<StatefulApiResponse<StateOperationInfoApiResponse>> SetSort([FromBody] SetSortApiRequest request)
    {
        SessionCommandResult<StateOperationInfoApiResponse> result = _sessionStateProcessor.SetSort(request);
        return ToStatefulActionResult(result);
    }

    [HttpPost("directories/entries/query")]
    /// <summary>
    /// 取得遞迴目錄清單（由客戶端傳入 state）。
    /// </summary>
    public ActionResult<StatefulApiResponse<DirectoryEntriesApiResponse>> ListDirectoryEntries([FromBody] StatefulDirectoryEntriesApiRequest request)
    {
        SessionCommandResult<DirectoryEntriesApiResponse> result = _sessionStateProcessor.GetDirectoryEntries(request);
        return ToStatefulActionResult(result);
    }
}
