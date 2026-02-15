using CloudFileManager.Application.Interfaces;
using CloudFileManager.Application.Models;

namespace CloudFileManager.Application.Implementations;

/// <summary>
/// LocalFileUploadRequestFactory 類別，負責由本機檔案建立上傳請求。
/// </summary>
public sealed class LocalFileUploadRequestFactory : ILocalFileUploadRequestFactory
{
    private readonly IReadOnlyList<IFileMetadataExtractor> _extractors;

    public LocalFileUploadRequestFactory(IEnumerable<IFileMetadataExtractor> extractors)
    {
        _extractors = extractors.ToList();
    }

    /// <summary>
    /// 由本機檔案建立上傳請求 DTO。
    /// </summary>
    public UploadFileRequest Create(string targetDirectoryPath, string originalFileName, string localPath)
    {
        string fileName = Path.GetFileName(originalFileName);
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        long fileSize = new FileInfo(localPath).Length;

        IFileMetadataExtractor? extractor = _extractors.FirstOrDefault(item => item.CanHandle(extension));
        if (extractor is null)
        {
            throw new InvalidOperationException($"Unsupported file type: {extension}");
        }

        return extractor.BuildRequest(targetDirectoryPath, fileName, fileSize, localPath);
    }
}
