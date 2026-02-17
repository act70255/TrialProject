using System.Xml.Linq;
using System.Globalization;
using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Interfaces;
using CloudFileManager.Application.Models;
using CloudFileManager.Domain;

namespace CloudFileManager.Application.Implementations;

/// <summary>
/// CloudFileReadModelService 類別，負責查詢與輸出相關讀取模型。
/// </summary>
public sealed class CloudFileReadModelService : ICloudFileReadModelService
{
    private readonly CloudDirectory _root;
    private readonly AppConfig _config;
    private readonly string _basePath;
    private readonly IXmlOutputWriter _xmlOutputWriter;

    public CloudFileReadModelService(CloudDirectory root, AppConfig config, string basePath, IXmlOutputWriter xmlOutputWriter)
    {
        _root = root;
        _config = config;
        _basePath = basePath;
        _xmlOutputWriter = xmlOutputWriter;
    }

    /// <summary>
    /// 取得目錄樹資料。
    /// </summary>
    public DirectoryTreeResult GetDirectoryTree()
    {
        List<string> lines = new();
        DirectoryNodeResult rootNode = BuildDirectoryNodeWithLines(_root, string.Empty, isLast: true, isRoot: true, lines);
        return new DirectoryTreeResult(lines, rootNode);
    }

    /// <summary>
    /// 計算目錄總容量。
    /// </summary>
    public SizeCalculationResult CalculateTotalSize(CalculateSizeRequest request)
    {
        CloudDirectory? directory = CloudFileTreeLookup.FindDirectory(_root, request.DirectoryPath);
        if (directory is null)
        {
            return new SizeCalculationResult(false, 0, ByteSizeFormatter.Format(0), ["Directory not found."]);
        }

        List<string> traverseLog = new();
        long bytes = directory.CalculateTotalBytes(traverseLog, request.DirectoryPath.Trim('/'));

        if (!_config.Logging.EnableTraverseLog)
        {
            traverseLog.Clear();
        }

        return new SizeCalculationResult(true, bytes, ByteSizeFormatter.Format(bytes), traverseLog);
    }

    /// <summary>
    /// 依副檔名搜尋檔案。
    /// </summary>
    public SearchResult SearchByExtension(SearchByExtensionRequest request)
    {
        string extension = ConfigDefaults.NormalizeExtension(request.Extension);
        string directoryPath = string.IsNullOrWhiteSpace(request.DirectoryPath) ? "Root" : request.DirectoryPath;
        CloudDirectory? directory = CloudFileTreeLookup.FindDirectory(_root, directoryPath);
        if (directory is null)
        {
            return new SearchResult([], ["Directory not found."]);
        }

        List<string> paths = new();
        List<string> log = new();

        SearchInDirectory(directory, directoryPath.Trim('/'), extension, paths, log);

        if (!_config.Logging.EnableTraverseLog)
        {
            log.Clear();
        }

        return new SearchResult(paths, log);
    }

    public DirectoryEntriesResult GetDirectoryEntries(ListDirectoryEntriesRequest request)
    {
        CloudDirectory? directory = CloudFileTreeLookup.FindDirectory(_root, request.DirectoryPath);
        if (directory is null)
        {
            return new DirectoryEntriesResult(false, []);
        }

        List<DirectoryEntryResult> entries = [];
        int order = 0;
        AppendDirectoryEntriesRecursively(directory, request.DirectoryPath.TrimEnd('/'), entries, ref order);

        return new DirectoryEntriesResult(true, entries);
    }

    private static void AppendDirectoryEntriesRecursively(
        CloudDirectory directory,
        string directoryPath,
        ICollection<DirectoryEntryResult> entries,
        ref int order)
    {
        foreach (CloudDirectory childDirectory in directory.Directories)
        {
            order++;
            string childPath = $"{directoryPath}/{childDirectory.Name}";
            long sizeBytes = childDirectory.CalculateTotalBytes();
            entries.Add(new DirectoryEntryResult(
                childDirectory.Name,
                IsDirectory: true,
                FullPath: childPath,
                SizeBytes: sizeBytes,
                FormattedSize: ByteSizeFormatter.Format(sizeBytes),
                Extension: string.Empty,
                SiblingOrder: order));

            AppendDirectoryEntriesRecursively(childDirectory, childPath, entries, ref order);
        }

        foreach (CloudFile childFile in directory.Files)
        {
            order++;
            string childPath = $"{directoryPath}/{childFile.Name}";
            entries.Add(new DirectoryEntryResult(
                childFile.Name,
                IsDirectory: false,
                FullPath: childPath,
                SizeBytes: childFile.Size,
                FormattedSize: ByteSizeFormatter.Format(childFile.Size),
                Extension: Path.GetExtension(childFile.Name),
                SiblingOrder: order));
        }
    }

    /// <summary>
    /// 匯出目錄樹 XML。
    /// </summary>
    public XmlExportResult ExportXml(ExportXmlRequest? request = null)
    {
        string directoryPath = string.IsNullOrWhiteSpace(request?.DirectoryPath) ? "Root" : request.DirectoryPath;
        CloudDirectory? directory = CloudFileTreeLookup.FindDirectory(_root, directoryPath);
        if (directory is null)
        {
            throw new InvalidOperationException($"Directory not found: {directoryPath}");
        }

        XElement rootElement = BuildDirectoryElement(directory);
        XDocument document = new(new XDeclaration("1.0", "utf-8", "yes"), rootElement);
        string xmlContent = document.ToString();

        if (string.Equals(_config.Output.XmlTarget, "File", StringComparison.OrdinalIgnoreCase))
        {
            string fullPath = Path.IsPathRooted(_config.Output.XmlOutputPath)
                ? _config.Output.XmlOutputPath
                : Path.GetFullPath(Path.Combine(_basePath, _config.Output.XmlOutputPath));

            string outputPath = _xmlOutputWriter.Write(fullPath, xmlContent);
            return new XmlExportResult(xmlContent, outputPath);
        }

        return new XmlExportResult(xmlContent, null);
    }

    /// <summary>
    /// 取得功能旗標設定。
    /// </summary>
    public FeatureFlagsResult GetFeatureFlags()
    {
        Dictionary<string, bool> flags = new(StringComparer.Ordinal)
        {
        };

        return new FeatureFlagsResult(flags);
    }

    private static XElement BuildDirectoryElement(CloudDirectory directory)
    {
        XElement element = new("Directory", new XAttribute("Name", directory.Name));

        foreach (CloudFile file in directory.Files)
        {
            element.Add(new XElement("File",
                new XAttribute("Name", file.Name),
                new XAttribute("Size", ByteSizeFormatter.Format(file.Size)),
                new XAttribute("CreatedTime", FormatFileCreatedTimeForXml(file.CreatedTime)),
                new XAttribute("Type", file.FileType),
                new XAttribute("Detail", file.DetailText)));
        }

        foreach (CloudDirectory child in directory.Directories)
        {
            element.Add(BuildDirectoryElement(child));
        }

        return element;
    }

    private static DirectoryNodeResult BuildDirectoryNodeWithLines(CloudDirectory directory, string indent, bool isLast, bool isRoot, ICollection<string> lines)
    {
        string connector = isRoot ? string.Empty : (isLast ? "└── " : "├── ");
        lines.Add($"{indent}{connector}{directory.Name} [目錄]");

        string childIndent = isRoot
            ? string.Empty
            : indent + (isLast ? "    " : "│   ");

        List<FileNodeResult> files = new(directory.Files.Count);
        IReadOnlyList<CloudDirectory> childDirectories = directory.Directories;
        IReadOnlyList<CloudFile> childFiles = directory.Files;
        int totalChildren = childDirectories.Count + childFiles.Count;

        List<DirectoryNodeResult> directories = new(childDirectories.Count);
        for (int index = 0; index < childDirectories.Count; index++)
        {
            bool childIsLast = index == totalChildren - 1;
            directories.Add(BuildDirectoryNodeWithLines(childDirectories[index], childIndent, childIsLast, isRoot: false, lines));
        }

        for (int index = 0; index < childFiles.Count; index++)
        {
            CloudFile file = childFiles[index];
            bool childIsLast = (childDirectories.Count + index) == totalChildren - 1;
            string fileConnector = childIsLast ? "└── " : "├── ";
            lines.Add($"{childIndent}{fileConnector}{file.Name} [{FormatFileType(file)}] ({FormatFileDetail(file)}, 大小: {FormatFileSize(file.Size)}, 建立時間: {FormatFileCreatedTimeForDisplay(file.CreatedTime)})");
            files.Add(new FileNodeResult(file.Name));
        }

        return new DirectoryNodeResult(directory.Name, directories, files);
    }

    private static string FormatFileType(CloudFile file)
    {
        return file switch
        {
            WordFile => "Word 檔案",
            ImageFile => "圖片",
            TextFile => "純文字檔",
            _ => file.FileType.ToString()
        };
    }

    private static string FormatFileDetail(CloudFile file)
    {
        return file switch
        {
            WordFile wordFile => $"頁數: {wordFile.PageCount}",
            ImageFile imageFile => $"解析度: {imageFile.Width}x{imageFile.Height}",
            TextFile textFile => $"編碼: {textFile.Encoding}",
            _ => file.DetailText
        };
    }

    private static string FormatFileSize(long sizeInBytes)
    {
        return ByteSizeFormatter.Format(sizeInBytes);
    }

    private static string FormatFileCreatedTimeForDisplay(DateTime createdTime)
    {
        DateTime localTime = ConvertUtcToLocal(createdTime);
        return localTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static string FormatFileCreatedTimeForXml(DateTime createdTime)
    {
        DateTime localTime = ConvertUtcToLocal(createdTime);
        DateTimeOffset localOffsetTime = new(localTime);
        return localOffsetTime.ToString("O");
    }

    private static DateTime ConvertUtcToLocal(DateTime value)
    {
        DateTime utcTime = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return utcTime.ToLocalTime();
    }

    private static void SearchInDirectory(CloudDirectory directory, string currentPath, string extension, ICollection<string> paths, ICollection<string> log)
    {
        log.Add($"[Directory] {currentPath}");

        foreach (CloudFile file in directory.Files)
        {
            string fullPath = $"{currentPath}/{file.Name}";
            log.Add($"[File] {fullPath} ({file.GetType().Name})");
            if (Path.GetExtension(file.Name).Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                paths.Add(fullPath);
            }
        }

        foreach (CloudDirectory child in directory.Directories)
        {
            SearchInDirectory(child, $"{currentPath}/{child.Name}", extension, paths, log);
        }
    }

}
