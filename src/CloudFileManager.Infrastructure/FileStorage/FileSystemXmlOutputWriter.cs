using CloudFileManager.Application.Interfaces;
using System.Globalization;

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

        string uniquePath = BuildTimestampedPath(fullPath);
        File.WriteAllText(uniquePath, xmlContent);
        return uniquePath;
    }

    private static string BuildTimestampedPath(string fullPath)
    {
        string directoryPath = Path.GetDirectoryName(fullPath) ?? AppContext.BaseDirectory;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
        string extension = Path.GetExtension(fullPath);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff", CultureInfo.InvariantCulture);

        string candidatePath = Path.Combine(directoryPath, $"{fileNameWithoutExtension}_{timestamp}{extension}");
        if (!File.Exists(candidatePath))
        {
            return candidatePath;
        }

        int suffix = 1;
        while (true)
        {
            string retryPath = Path.Combine(directoryPath, $"{fileNameWithoutExtension}_{timestamp}_{suffix}{extension}");
            if (!File.Exists(retryPath))
            {
                return retryPath;
            }

            suffix++;
        }
    }
}
