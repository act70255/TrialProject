namespace CloudFileManager.Infrastructure.Configuration;

/// <summary>
/// ConfigPathResolver 類別，負責解析設定檔與執行階段基底路徑。
/// </summary>
public static class ConfigPathResolver
{
    /// <summary>
    /// 解析設定檔完整路徑。
    /// </summary>
    public static string ResolveConfigFilePath(string firstSearchPath, string fallbackSearchPath, string configFileName = "appsettings.json")
    {
        string? projectPath = FindProjectPathWithConfig(firstSearchPath, configFileName)
            ?? FindProjectPathWithConfig(fallbackSearchPath, configFileName);

        if (!string.IsNullOrWhiteSpace(projectPath))
        {
            return Path.Combine(projectPath, configFileName);
        }

        string firstCandidate = Path.Combine(firstSearchPath, configFileName);
        if (File.Exists(firstCandidate))
        {
            return firstCandidate;
        }

        return Path.Combine(AppContext.BaseDirectory, configFileName);
    }

    /// <summary>
    /// 解析執行階段基底路徑。
    /// </summary>
    public static string ResolveRuntimeBasePath(string configFilePath, string firstSearchPath, string fallbackSearchPath, string solutionFileName)
    {
        string? solutionRoot = FindAncestorWithFile(firstSearchPath, solutionFileName)
            ?? FindAncestorWithFile(fallbackSearchPath, solutionFileName);

        if (!string.IsNullOrWhiteSpace(solutionRoot))
        {
            return solutionRoot;
        }

        return Path.GetDirectoryName(configFilePath) ?? AppContext.BaseDirectory;
    }

    private static string? FindProjectPathWithConfig(string startPath, string configFileName)
    {
        DirectoryInfo? directory = new(startPath);
        while (directory is not null)
        {
            string configPath = Path.Combine(directory.FullName, configFileName);
            bool hasProjectFile = directory.GetFiles("*.csproj", SearchOption.TopDirectoryOnly).Length > 0;
            if (File.Exists(configPath) && hasProjectFile)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string? FindAncestorWithFile(string startPath, string fileName)
    {
        DirectoryInfo? directory = new(startPath);
        while (directory is not null)
        {
            string filePath = Path.Combine(directory.FullName, fileName);
            if (File.Exists(filePath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
