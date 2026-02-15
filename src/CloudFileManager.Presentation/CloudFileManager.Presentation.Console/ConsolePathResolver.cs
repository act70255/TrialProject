namespace CloudFileManager.Presentation.ConsoleApp;

/// <summary>
/// ConsolePathResolver 類別，負責解析 Console 相對與絕對路徑。
/// </summary>
public static class ConsolePathResolver
{
    /// <summary>
    /// 解析路徑。
    /// </summary>
    public static string Resolve(string rawPath, string currentDirectoryPath)
    {
        string normalizedRaw = rawPath.Replace('\\', '/').Trim();
        if (string.IsNullOrWhiteSpace(normalizedRaw) || normalizedRaw == ".")
        {
            return currentDirectoryPath;
        }

        bool isAbsolute = normalizedRaw.StartsWith("Root", StringComparison.OrdinalIgnoreCase) || normalizedRaw.StartsWith('/');
        List<string> segments = isAbsolute
            ? ["Root"]
            : currentDirectoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        foreach (string segment in normalizedRaw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count > 1)
                {
                    segments.RemoveAt(segments.Count - 1);
                }

                continue;
            }

            if (segments.Count == 0)
            {
                segments.Add("Root");
            }

            if (segments.Count == 1 && segments[0].Equals("Root", StringComparison.OrdinalIgnoreCase) && segment.Equals("Root", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            segments.Add(segment);
        }

        if (segments.Count == 0)
        {
            return "Root";
        }

        segments[0] = "Root";
        return string.Join('/', segments);
    }
}
