using CloudFileManager.Application.Models;

namespace CloudFileManager.Presentation.ConsoleApp;

/// <summary>
/// ConsoleDirectorySnapshotBuilder 類別，負責由目錄樹輸出建立快照。
/// </summary>
public static class ConsoleDirectorySnapshotBuilder
{
    /// <summary>
    /// 建立目錄快照。
    /// </summary>
    public static ConsoleDirectorySnapshot Build(DirectoryTreeResult tree)
    {
        HashSet<string> directoryPaths = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<string>> directoryChildren = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<string>> fileChildren = new(StringComparer.OrdinalIgnoreCase);
        AddDirectoryRecursively(tree.Root, tree.Root.Name, directoryPaths, directoryChildren, fileChildren);

        return new ConsoleDirectorySnapshot(directoryPaths, directoryChildren, fileChildren);
    }

    private static void AddDirectoryRecursively(
        DirectoryNodeResult directory,
        string path,
        ISet<string> directoryPaths,
        IDictionary<string, List<string>> directoryChildren,
        IDictionary<string, List<string>> fileChildren)
    {
        directoryPaths.Add(path);

        if (!directoryChildren.TryGetValue(path, out List<string>? directoryItems))
        {
            directoryItems = [];
            directoryChildren[path] = directoryItems;
        }

        if (!fileChildren.TryGetValue(path, out List<string>? fileItems))
        {
            fileItems = [];
            fileChildren[path] = fileItems;
        }

        foreach (DirectoryNodeResult childDirectory in directory.Directories)
        {
            directoryItems.Add(childDirectory.Name);
            string childPath = $"{path}/{childDirectory.Name}";
            AddDirectoryRecursively(childDirectory, childPath, directoryPaths, directoryChildren, fileChildren);
        }

        foreach (FileNodeResult file in directory.Files)
        {
            fileItems.Add(file.Name);
        }
    }
}

/// <summary>
/// ConsoleDirectorySnapshot 類別，負責封裝目錄樹快照。
/// </summary>
public sealed record ConsoleDirectorySnapshot(
    HashSet<string> DirectoryPaths,
    Dictionary<string, List<string>> DirectoryChildren,
    Dictionary<string, List<string>> FileChildren);
