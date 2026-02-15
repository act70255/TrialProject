using System.IO.Compression;
using System.Xml.Linq;
using CloudFileManager.Application.Models;

namespace CloudFileManager.Application.Implementations;

public sealed class WordFileMetadataExtractor : IFileMetadataExtractor
{
    public bool CanHandle(string extension)
    {
        return extension.Equals(".docx", StringComparison.OrdinalIgnoreCase);
    }

    public UploadFileRequest BuildRequest(string targetDirectoryPath, string fileName, long fileSize, string localPath)
    {
        return new UploadFileRequest(
            targetDirectoryPath,
            fileName,
            fileSize,
            PageCount: ReadPageCount(localPath),
            SourceLocalPath: localPath);
    }

    private static int ReadPageCount(string localPath)
    {
        using ZipArchive archive = ZipFile.OpenRead(localPath);
        ZipArchiveEntry? appXmlEntry = archive.GetEntry("docProps/app.xml");
        if (appXmlEntry is null)
        {
            return 1;
        }

        using Stream stream = appXmlEntry.Open();
        XDocument document = XDocument.Load(stream);
        XElement? pagesElement = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "Pages");
        if (pagesElement is null)
        {
            return 1;
        }

        return int.TryParse(pagesElement.Value, out int pageCount) && pageCount > 0 ? pageCount : 1;
    }
}
