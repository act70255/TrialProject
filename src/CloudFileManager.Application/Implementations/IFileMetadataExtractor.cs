using CloudFileManager.Application.Models;

namespace CloudFileManager.Application.Implementations;

public interface IFileMetadataExtractor
{
    bool CanHandle(string extension);

    UploadFileRequest BuildRequest(string targetDirectoryPath, string fileName, long fileSize, string localPath);
}
