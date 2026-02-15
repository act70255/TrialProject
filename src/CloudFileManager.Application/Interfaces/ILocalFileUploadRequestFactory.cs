using CloudFileManager.Application.Models;

namespace CloudFileManager.Application.Interfaces;

/// <summary>
/// ILocalFileUploadRequestFactory 介面契約。
/// </summary>
public interface ILocalFileUploadRequestFactory
{
    /// <summary>
    /// 由本機檔案建立上傳請求 DTO。
    /// </summary>
    UploadFileRequest Create(string targetDirectoryPath, string originalFileName, string localPath);
}
