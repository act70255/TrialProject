using CloudFileManager.Application.Models;
using System.Text;
using System.Xml.Linq;

namespace CloudFileManager.Presentation.ConsoleApp;

public sealed partial class ConsoleCommandExecutor
{
    private void PrintTree()
    {
        PrintSectionHeader("DIRECTORY TREE");
        foreach (string line in _service.GetDirectoryTree().Lines)
        {
            System.Console.WriteLine(line);
        }

        PrintSectionFooter("DIRECTORY TREE");
    }

    private void HandleSize(IReadOnlyList<string> args)
    {
        string path = args.Count == 0 ? CurrentDirectoryPath : ResolveDirectoryPath(args[0]);
        SizeCalculationResult result = _service.CalculateTotalSize(new CalculateSizeRequest(path));
        if (!result.IsFound)
        {
            PrintError($"Directory not found: {path}");
            return;
        }

        PrintSectionHeader("SIZE RESULT");
        System.Console.WriteLine($"[RESULT] Size: {result.FormattedSize}");
        PrintSectionFooter("SIZE RESULT");
        PrintTraverseLog(result.TraverseLog, []);
    }

    private void HandleSearch(IReadOnlyList<string> args)
    {
        if (args.Count < 1 || args.Count > 2)
        {
            PrintUsage("search <extension> [directoryPath]");
            return;
        }

        string directoryPath = args.Count == 1 ? CurrentDirectoryPath : ResolveDirectoryPath(args[1]);
        SearchResult result = _service.SearchByExtension(new SearchByExtensionRequest(args[0], directoryPath));
        PrintSectionHeader("MATCHED FILES");
        if (result.Paths.Count == 0)
        {
            System.Console.WriteLine("[RESULT] No match.");
            PrintSectionFooter("MATCHED FILES");
            PrintTraverseLog(result.TraverseLog, []);
            return;
        }

        foreach (string path in result.Paths)
        {
            System.Console.WriteLine(path);
        }

        PrintSectionFooter("MATCHED FILES");

        PrintTraverseLog(result.TraverseLog, result.Paths);
    }

    private void HandleListRecursive(IReadOnlyList<string> args)
    {
        if (args.Count > 1)
        {
            PrintUsage("lsr [directoryPath]");
            return;
        }

        string directoryPath = args.Count == 0 ? CurrentDirectoryPath : ResolveDirectoryPath(args[0]);
        DirectoryEntriesResult result = _service.GetDirectoryEntries(new ListDirectoryEntriesRequest(directoryPath));
        if (!result.IsFound)
        {
            PrintError($"Directory not found: {directoryPath}");
            return;
        }

        ConsoleSortState? sortState = _sessionState.CurrentSortState;
        IEnumerable<DirectoryEntryResult> entries = sortState is null
            ? result.Entries
            : ApplySortEntries(result.Entries, sortState.Key, sortState.Direction);

        PrintSectionHeader("LIST RECURSIVE");
        System.Console.WriteLine($"[RESULT] Directory: {directoryPath}");
        if (sortState is not null)
        {
            PrintInfo($"Applied sort: {sortState.Key} {sortState.Direction}");
        }

        if (!entries.Any())
        {
            System.Console.WriteLine("[RESULT] (empty)");
            PrintSectionFooter("LIST RECURSIVE");
            return;
        }

        foreach (DirectoryEntryResult entry in entries)
        {
            if (entry.IsDirectory)
            {
                System.Console.WriteLine($"[Dir]  {entry.FullPath} (size: {entry.FormattedSize})");
                continue;
            }

            string ext = string.IsNullOrWhiteSpace(entry.Extension) ? "(none)" : entry.Extension;
            System.Console.WriteLine($"[File] {entry.FullPath} (ext: {ext}, size: {entry.FormattedSize})");
        }

        PrintSectionFooter("LIST RECURSIVE");
    }

    private void HandleSort(IReadOnlyList<string> args)
    {
        if (args.Count != 2)
        {
            PrintUsage("sort <name|size|ext> <asc|desc>");
            return;
        }

        string key = args[0].ToLowerInvariant();
        string direction = args[1].ToLowerInvariant();
        if (key is not ("name" or "size" or "ext"))
        {
            PrintError($"Unsupported sort key: {args[0]}");
            return;
        }

        if (direction is not ("asc" or "desc"))
        {
            PrintError($"Unsupported sort direction: {args[1]}");
            return;
        }

        string directoryPath = CurrentDirectoryPath;
        DirectoryEntriesResult result = _service.GetDirectoryEntries(new ListDirectoryEntriesRequest(directoryPath));
        if (!result.IsFound)
        {
            PrintError($"Directory not found: {directoryPath}");
            return;
        }

        IEnumerable<DirectoryEntryResult> sorted = ApplySortEntries(result.Entries, key, direction);

        ConsoleSortState? previousSortState = _sessionState.CurrentSortState;
        ConsoleSortState currentSortState = new(key, direction);
        _sessionState.CurrentSortState = currentSortState;
        if (!Equals(previousSortState, currentSortState))
        {
            RecordUndoAction(new ConsoleUndoAction(
                ConsoleUndoActionKind.SortSettingChanged,
                PreviousSortState: previousSortState,
                CurrentSortState: currentSortState));
        }

        PrintSectionHeader("SORT RESULT");
        System.Console.WriteLine($"[RESULT] Directory: {directoryPath}");
        System.Console.WriteLine("[RESULT] Scope: recursive (all descendants in current directory)");
        PrintInfo($"Sort setting saved globally: {key} {direction}");
        System.Console.WriteLine($"[RESULT] Key: {key}, Direction: {direction}");
        foreach (DirectoryEntryResult entry in sorted)
        {
            if (entry.IsDirectory)
            {
                System.Console.WriteLine($"[Dir]  {entry.FullPath} (size: {entry.FormattedSize})");
                continue;
            }

            string ext = string.IsNullOrWhiteSpace(entry.Extension) ? "(none)" : entry.Extension;
            System.Console.WriteLine($"[File] {entry.FullPath} (ext: {ext}, size: {entry.FormattedSize})");
        }

        PrintSectionFooter("SORT RESULT");
    }

    private static IEnumerable<DirectoryEntryResult> ApplySortEntries(IEnumerable<DirectoryEntryResult> entries, string key, string direction)
    {
        return key switch
        {
            "name" => direction == "asc"
                ? entries.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.SiblingOrder)
                : entries.OrderByDescending(item => item.Name, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.SiblingOrder),
            "size" => direction == "asc"
                ? entries.OrderBy(item => item.SizeBytes).ThenBy(item => item.SiblingOrder)
                : entries.OrderByDescending(item => item.SizeBytes).ThenBy(item => item.SiblingOrder),
            _ => direction == "asc"
                ? entries.OrderBy(item => item.Extension, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.SiblingOrder)
                : entries.OrderByDescending(item => item.Extension, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.SiblingOrder)
        };
    }

    private void HandleXml(IReadOnlyList<string> args)
    {
        if (args.Count > 2)
        {
            PrintUsage("xml [directoryPath] [raw]");
            return;
        }

        bool raw = args.Any(arg => string.Equals(arg, "raw", StringComparison.OrdinalIgnoreCase));
        string? pathArg = args.FirstOrDefault(arg => !string.Equals(arg, "raw", StringComparison.OrdinalIgnoreCase));
        string directoryPath = string.IsNullOrWhiteSpace(pathArg) ? CurrentDirectoryPath : ResolveDirectoryPath(pathArg);
        XmlExportResult result = _service.ExportXml(new ExportXmlRequest(directoryPath));
        string xmlContentForCommand = raw ? result.XmlContent : ToSampleStyleXml(result.XmlContent);

        if (!string.IsNullOrWhiteSpace(result.OutputPath))
        {
            File.WriteAllText(result.OutputPath, xmlContentForCommand, Encoding.UTF8);
        }

        PrintSectionHeader("XML 輸出");
        System.Console.WriteLine(xmlContentForCommand);
        PrintSectionFooter("XML 輸出");
        if (!string.IsNullOrWhiteSpace(result.OutputPath))
        {
            System.Console.WriteLine($"[RESULT] 已儲存至：{result.OutputPath}");
        }
    }

    private static void PrintTraverseLog(IReadOnlyList<string> traverseLog, IReadOnlyCollection<string> matchedPaths)
    {
        if (traverseLog.Count == 0)
        {
            return;
        }

        HashSet<string> matchedPathSet = new(matchedPaths, StringComparer.OrdinalIgnoreCase);
        PrintSectionHeader("TRAVERSE LOG");
        foreach (string entry in traverseLog)
        {
            string? tracedFilePath = TryExtractFilePath(entry);
            if (tracedFilePath is not null && matchedPathSet.Contains(tracedFilePath))
            {
                System.Console.WriteLine($"TRACE [符合] {entry}");
                continue;
            }

            System.Console.WriteLine($"TRACE {entry}");
        }

        PrintSectionFooter("TRAVERSE LOG");
    }

    private static string? TryExtractFilePath(string traceEntry)
    {
        const string filePrefix = "[File] ";
        if (!traceEntry.StartsWith(filePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        int typeSeparator = traceEntry.IndexOf(" (", StringComparison.Ordinal);
        if (typeSeparator < 0)
        {
            return traceEntry[filePrefix.Length..].Trim();
        }

        return traceEntry[filePrefix.Length..typeSeparator].Trim();
    }

    private static string ToSampleStyleXml(string rawXml)
    {
        XDocument document = XDocument.Parse(rawXml);
        XElement? root = document.Root;
        if (root is null)
        {
            return rawXml;
        }

        StringBuilder builder = new();
        AppendSampleStyleElement(root, builder, 0, isRoot: true);
        return builder.ToString();
    }

    private static void AppendSampleStyleElement(XElement node, StringBuilder builder, int depth, bool isRoot = false)
    {
        string indent = new(' ', depth * 4);
        if (node.Name.LocalName == "Directory")
        {
            string name = node.Attribute("Name")?.Value ?? "Unknown";
            string tag = isRoot ? $"根目錄_{NormalizeTag(name)}" : $"目錄_{NormalizeTag(name)}";
            builder.Append(indent).Append('<').Append(tag).AppendLine(">");

            foreach (XElement child in node.Elements())
            {
                AppendSampleStyleElement(child, builder, depth + 1);
            }

            builder.Append(indent).Append("</").Append(tag).AppendLine(">");
            return;
        }

        if (node.Name.LocalName == "File")
        {
            string fileName = node.Attribute("Name")?.Value ?? "Unknown";
            string size = node.Attribute("Size")?.Value ?? "0KB";
            string detail = node.Attribute("Detail")?.Value ?? string.Empty;
            string tag = BuildFileTag(fileName);
            string content = $"{ConvertDetail(detail)}, 大小: {size}";
            builder.Append(indent)
                .Append('<')
                .Append(tag)
                .Append('>')
                .Append(content)
                .Append("</")
                .Append(tag)
                .AppendLine(">");
        }
    }

    private static string BuildFileTag(string fileName)
    {
        string extension = Path.GetExtension(fileName).TrimStart('.');
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return NormalizeTag(nameWithoutExtension);
        }

        return $"{NormalizeTag(nameWithoutExtension)}_{NormalizeTag(extension)}";
    }

    private static string NormalizeTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown";
        }

        StringBuilder builder = new();
        foreach (char ch in value)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                builder.Append(ch);
                continue;
            }

            builder.Append('_');
        }

        return builder.ToString().Trim('_');
    }

    private static string ConvertDetail(string detail)
    {
        if (detail.StartsWith("PageCount=", StringComparison.OrdinalIgnoreCase))
        {
            return $"頁數: {detail["PageCount=".Length..]}";
        }

        if (detail.StartsWith("Resolution=", StringComparison.OrdinalIgnoreCase))
        {
            return $"解析度: {detail["Resolution=".Length..]}";
        }

        if (detail.StartsWith("Encoding=", StringComparison.OrdinalIgnoreCase))
        {
            return $"編碼: {detail["Encoding=".Length..]}";
        }

        return detail;
    }
}
