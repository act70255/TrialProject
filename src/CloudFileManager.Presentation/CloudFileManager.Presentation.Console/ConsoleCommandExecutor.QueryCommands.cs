using CloudFileManager.Application.Models;

namespace CloudFileManager.Presentation.ConsoleApp;

public sealed partial class ConsoleCommandExecutor
{
    private void PrintTree()
    {
        foreach (string line in _service.GetDirectoryTree().Lines)
        {
            System.Console.WriteLine(line);
        }
    }

    private void HandleSize(IReadOnlyList<string> args)
    {
        string path = args.Count == 0 ? CurrentDirectoryPath : ResolveDirectoryPath(args[0]);
        SizeCalculationResult result = _service.CalculateTotalSize(new CalculateSizeRequest(path));
        if (!result.IsFound)
        {
            System.Console.WriteLine($"Directory not found: {path}");
            return;
        }

        System.Console.WriteLine($"Size: {result.FormattedSize} ({result.SizeBytes} Bytes)");
    }

    private void HandleSearch(IReadOnlyList<string> args)
    {
        if (args.Count < 1)
        {
            System.Console.WriteLine("Usage: search <extension>");
            return;
        }

        SearchResult result = _service.SearchByExtension(new SearchByExtensionRequest(args[0]));
        if (result.Paths.Count == 0)
        {
            System.Console.WriteLine("No match.");
            return;
        }

        foreach (string path in result.Paths)
        {
            System.Console.WriteLine(path);
        }
    }

    private void HandleXml(IReadOnlyList<string> _)
    {
        XmlExportResult result = _service.ExportXml();
        System.Console.WriteLine(result.XmlContent);
        if (!string.IsNullOrWhiteSpace(result.OutputPath))
        {
            System.Console.WriteLine($"Saved: {result.OutputPath}");
        }
    }
}
