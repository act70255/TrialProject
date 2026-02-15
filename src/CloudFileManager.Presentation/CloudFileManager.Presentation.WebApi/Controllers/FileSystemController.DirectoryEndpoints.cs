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
}
