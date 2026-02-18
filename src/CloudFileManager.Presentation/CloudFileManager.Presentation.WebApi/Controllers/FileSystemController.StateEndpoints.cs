using CloudFileManager.Presentation.WebApi.Model;
using Microsoft.AspNetCore.Mvc;

namespace CloudFileManager.Presentation.WebApi.Controllers;

public sealed partial class FileSystemController
{
    [HttpPost("clipboard/copy")]
    /// <summary>
    /// 複製節點到剪貼簿（由客戶端傳入 state）。
    /// </summary>
    public ActionResult<StatefulApiResponse<StateOperationInfoApiResponse>> ClipboardCopy([FromBody] ClipboardCopyApiRequest request)
    {
        SessionCommandResult<StateOperationInfoApiResponse> result = _sessionStateProcessor.CopyToClipboard(request);
        return ToStatefulActionResult(result);
    }

    [HttpPost("clipboard/paste")]
    /// <summary>
    /// 貼上剪貼簿內容（由客戶端傳入 state）。
    /// </summary>
    public ActionResult<StatefulApiResponse<StateOperationInfoApiResponse>> ClipboardPaste([FromBody] ClipboardPasteApiRequest request)
    {
        SessionCommandResult<StateOperationInfoApiResponse> result = _sessionStateProcessor.PasteFromClipboard(request);
        return ToStatefulActionResult(result);
    }

    [HttpPost("tags/assign")]
    /// <summary>
    /// 指派標籤到節點（由客戶端傳入 state）。
    /// </summary>
    public ActionResult<StatefulApiResponse<StateOperationInfoApiResponse>> AssignTag([FromBody] TagAssignApiRequest request)
    {
        SessionCommandResult<StateOperationInfoApiResponse> result = _sessionStateProcessor.AssignTag(request);
        return ToStatefulActionResult(result);
    }

    [HttpPost("tags/remove")]
    /// <summary>
    /// 移除節點標籤（由客戶端傳入 state）。
    /// </summary>
    public ActionResult<StatefulApiResponse<StateOperationInfoApiResponse>> RemoveTag([FromBody] TagRemoveApiRequest request)
    {
        SessionCommandResult<StateOperationInfoApiResponse> result = _sessionStateProcessor.RemoveTag(request);
        return ToStatefulActionResult(result);
    }

    [HttpPost("tags/list")]
    /// <summary>
    /// 查詢標籤列表（由客戶端傳入 state）。
    /// </summary>
    public ActionResult<StatefulApiResponse<TagListApiResponse>> ListTags([FromBody] TagListApiRequest request)
    {
        SessionCommandResult<TagListApiResponse> result = _sessionStateProcessor.ListTags(request);
        return ToStatefulActionResult(result);
    }

    [HttpPost("tags/find")]
    /// <summary>
    /// 依標籤查詢節點（由客戶端傳入 state）。
    /// </summary>
    public ActionResult<StatefulApiResponse<TagFindResultApiResponse>> FindTags([FromBody] TagFindApiRequest request)
    {
        SessionCommandResult<TagFindResultApiResponse> result = _sessionStateProcessor.FindTags(request);
        return ToStatefulActionResult(result);
    }

    [HttpPost("history/undo")]
    /// <summary>
    /// 執行 Undo（由客戶端傳入 state）。
    /// </summary>
    public ActionResult<StatefulApiResponse<StateOperationInfoApiResponse>> Undo([FromBody] HistoryActionApiRequest request)
    {
        SessionCommandResult<StateOperationInfoApiResponse> result = _sessionStateProcessor.Undo(request);
        return ToStatefulActionResult(result);
    }

    [HttpPost("history/redo")]
    /// <summary>
    /// 執行 Redo（由客戶端傳入 state）。
    /// </summary>
    public ActionResult<StatefulApiResponse<StateOperationInfoApiResponse>> Redo([FromBody] HistoryActionApiRequest request)
    {
        SessionCommandResult<StateOperationInfoApiResponse> result = _sessionStateProcessor.Redo(request);
        return ToStatefulActionResult(result);
    }

    [HttpPost("search/query")]
    /// <summary>
    /// 執行搜尋（由客戶端傳入 state）。
    /// </summary>
    public ActionResult<StatefulApiResponse<SearchApiResponse>> SearchWithState([FromBody] StatefulSearchApiRequest request)
    {
        SessionCommandResult<SearchApiResponse> result = _sessionStateProcessor.Search(request);
        return ToStatefulActionResult(result);
    }

    [HttpPost("xml/export")]
    /// <summary>
    /// 匯出 XML（由客戶端傳入 state）。
    /// </summary>
    public ActionResult<StatefulApiResponse<XmlExportApiResponse>> ExportXmlWithState([FromBody] StatefulXmlExportApiRequest request)
    {
        SessionCommandResult<XmlExportApiResponse> result = _sessionStateProcessor.ExportXml(request);
        return ToStatefulActionResult(result);
    }
}
