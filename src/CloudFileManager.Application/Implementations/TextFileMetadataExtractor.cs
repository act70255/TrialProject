using CloudFileManager.Application.Models;

namespace CloudFileManager.Application.Implementations;

public sealed class TextFileMetadataExtractor : IFileMetadataExtractor
{
    public bool CanHandle(string extension)
    {
        return extension.Equals(".txt", StringComparison.OrdinalIgnoreCase);
    }

    public UploadFileRequest BuildRequest(string targetDirectoryPath, string fileName, long fileSize, string localPath)
    {
        return new UploadFileRequest(
            targetDirectoryPath,
            fileName,
            fileSize,
            Encoding: DetectEncoding(localPath),
            SourceLocalPath: localPath);
    }

    private static string DetectEncoding(string localPath)
    {
        byte[] prefix = new byte[4];
        using FileStream stream = File.OpenRead(localPath);
        int bytesRead = stream.Read(prefix, 0, prefix.Length);

        if (bytesRead >= 3 && prefix[0] == 0xEF && prefix[1] == 0xBB && prefix[2] == 0xBF)
        {
            return "UTF-8";
        }

        if (bytesRead >= 2 && prefix[0] == 0xFF && prefix[1] == 0xFE)
        {
            return "UTF-16LE";
        }

        if (bytesRead >= 2 && prefix[0] == 0xFE && prefix[1] == 0xFF)
        {
            return "UTF-16BE";
        }

        return "UTF-8";
    }
}
