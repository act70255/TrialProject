using CloudFileManager.Application.Interfaces;

namespace CloudFileManager.Infrastructure.FileStorage;

public sealed class FileSystemXmlOutputWriter : IXmlOutputWriter
{
    public string Write(string outputPath, string xmlContent)
    {
        string fullPath = Path.GetFullPath(outputPath);
        string? directoryPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(fullPath, xmlContent);
        return fullPath;
    }
}
