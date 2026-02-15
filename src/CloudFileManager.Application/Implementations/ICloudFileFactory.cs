using CloudFileManager.Application.Models;
using CloudFileManager.Domain;

namespace CloudFileManager.Application.Implementations;

/// <summary>
/// ICloudFileFactory 介面契約。
/// </summary>
public interface ICloudFileFactory
{
    /// <summary>
    /// 判斷是否可建立對應檔案實體。
    /// </summary>
    bool CanCreate(string extension);

    /// <summary>
    /// 建立對應的檔案實體。
    /// </summary>
    CloudFile Create(UploadFileRequest request, DateTime createdTime);
}
