using CloudFileManager.Application.Models;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using CloudFileManager.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace CloudFileManager.Infrastructure.FileStorage;

public sealed partial class StorageMetadataGateway
{
    public OperationResult AssignTag(string path, string tagName)
    {
        string normalizedPath = NormalizePath(path);
        TagEntity? tag = FindTagByName(tagName);
        if (tag is null)
        {
            return new OperationResult(false, $"Unsupported tag: {tagName}", OperationErrorCodes.ValidationFailed);
        }

        if (TryResolveDirectoryAndFile(normalizedPath, out DirectoryEntity? directory, out FileEntity? file) is false)
        {
            return new OperationResult(false, $"Node not found: {normalizedPath}", OperationErrorCodes.ResourceNotFound);
        }

        bool exists = directory is not null
            ? _dbContext.NodeTags.Any(item => item.DirectoryId == directory.Id && item.TagId == tag.Id)
            : _dbContext.NodeTags.Any(item => item.FileId == file!.Id && item.TagId == tag.Id);

        if (exists)
        {
            return new OperationResult(true, $"Tag already exists: {tag.Name} -> {normalizedPath}");
        }

        _dbContext.NodeTags.Add(new NodeTagEntity
        {
            Id = Guid.NewGuid(),
            TagId = tag.Id,
            DirectoryId = directory?.Id,
            FileId = file?.Id,
            CreatedTime = DateTime.UtcNow
        });
        _dbContext.SaveChanges();
        return new OperationResult(true, $"Tag assigned: {tag.Name} ({tag.Color}) -> {normalizedPath}");
    }

    public OperationResult RemoveTag(string path, string tagName)
    {
        string normalizedPath = NormalizePath(path);
        TagEntity? tag = FindTagByName(tagName);
        if (tag is null)
        {
            return new OperationResult(false, $"Unsupported tag: {tagName}", OperationErrorCodes.ValidationFailed);
        }

        if (TryResolveDirectoryAndFile(normalizedPath, out DirectoryEntity? directory, out FileEntity? file) is false)
        {
            return new OperationResult(false, $"Node not found: {normalizedPath}", OperationErrorCodes.ResourceNotFound);
        }

        NodeTagEntity? nodeTag = directory is not null
            ? _dbContext.NodeTags.FirstOrDefault(item => item.DirectoryId == directory.Id && item.TagId == tag.Id)
            : _dbContext.NodeTags.FirstOrDefault(item => item.FileId == file!.Id && item.TagId == tag.Id);
        if (nodeTag is null)
        {
            return new OperationResult(false, $"Tag not found on node: {tag.Name} -> {normalizedPath}", OperationErrorCodes.ResourceNotFound);
        }

        _dbContext.NodeTags.Remove(nodeTag);
        _dbContext.SaveChanges();
        return new OperationResult(true, $"Tag removed: {tag.Name} -> {normalizedPath}");
    }

    public TagListResult ListTags(string? path)
    {
        string? normalizedPath = string.IsNullOrWhiteSpace(path) ? null : NormalizePath(path);
        Dictionary<Guid, string> directoryPathMap = BuildDirectoryPathMap();
        Dictionary<Guid, string> filePathMap = BuildFilePathMap(directoryPathMap);

        List<NodeTagEntity> nodeTags = _dbContext.NodeTags
            .AsNoTracking()
            .Include(item => item.Tag)
            .ToList();

        Dictionary<string, List<TagInfoResult>> grouped = new(StringComparer.OrdinalIgnoreCase);
        foreach (NodeTagEntity nodeTag in nodeTags)
        {
            string? nodePath = ResolveNodePath(nodeTag, directoryPathMap, filePathMap);
            if (string.IsNullOrWhiteSpace(nodePath))
            {
                continue;
            }

            if (normalizedPath is not null && !nodePath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!grouped.TryGetValue(nodePath, out List<TagInfoResult>? tags))
            {
                tags = [];
                grouped[nodePath] = tags;
            }

            if (tags.Any(item => item.Name.Equals(nodeTag.Tag.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            tags.Add(new TagInfoResult(nodeTag.Tag.Name, nodeTag.Tag.Color));
        }

        List<TaggedNodeResult> items = grouped
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => new TaggedNodeResult(
                item.Key,
                item.Value
                    .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray()))
            .ToList();

        return new TagListResult(items);
    }

    public TagFindResult FindTaggedPaths(string tagName, string scopePath)
    {
        string normalizedScopePath = NormalizePath(scopePath);
        TagEntity? tag = FindTagByName(tagName);
        if (tag is null)
        {
            return new TagFindResult(tagName, string.Empty, normalizedScopePath, []);
        }

        TagListResult allTagResult = ListTags(null);
        List<string> paths = allTagResult.Items
            .Where(item => item.Tags.Any(nodeTag => nodeTag.Name.Equals(tag.Name, StringComparison.OrdinalIgnoreCase)))
            .Select(item => item.Path)
            .Where(path => IsPathInScope(path, normalizedScopePath))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new TagFindResult(tag.Name, tag.Color, normalizedScopePath, paths);
    }

    private bool TryResolveDirectoryAndFile(string normalizedPath, out DirectoryEntity? directory, out FileEntity? file)
    {
        directory = StoragePathLookupQueries.FindDirectoryByPath(_dbContext, normalizedPath);
        if (directory is not null)
        {
            file = null;
            return true;
        }

        (DirectoryEntity? _, FileEntity? resolvedFile) = StoragePathLookupQueries.FindFileByPath(_dbContext, normalizedPath);
        file = resolvedFile;
        return file is not null;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim().TrimEnd('/');
    }

    private TagEntity? FindTagByName(string tagName)
    {
        string normalizedTagName = tagName.Trim();

        if (StorageDbProviderClassifier.IsSqlite(_dbContext))
        {
            return _dbContext.Tags.FirstOrDefault(item => EF.Functions.Collate(item.Name, "NOCASE") == normalizedTagName);
        }

        if (StorageDbProviderClassifier.IsSqlServer(_dbContext))
        {
            return _dbContext.Tags.FirstOrDefault(item => EF.Functions.Collate(item.Name, "SQL_Latin1_General_CP1_CI_AS") == normalizedTagName);
        }

        return _dbContext.Tags.FirstOrDefault(item => item.Name == normalizedTagName);
    }

    private Dictionary<Guid, string> BuildDirectoryPathMap()
    {
        List<DirectoryEntity> directories = _dbContext.Directories
            .AsNoTracking()
            .Select(item => new DirectoryEntity
            {
                Id = item.Id,
                ParentId = item.ParentId,
                Name = item.Name
            })
            .ToList();

        DirectoryEntity? root = directories.FirstOrDefault(item => item.ParentId is null && item.Name == "Root");
        if (root is null)
        {
            return new Dictionary<Guid, string>();
        }

        Dictionary<Guid, List<DirectoryEntity>> children = directories
            .Where(item => item.ParentId.HasValue)
            .GroupBy(item => item.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        Dictionary<Guid, string> pathMap = new();
        BuildDirectoryPathMapRecursively(root, "Root", children, pathMap);
        return pathMap;
    }

    private static void BuildDirectoryPathMapRecursively(
        DirectoryEntity current,
        string path,
        IReadOnlyDictionary<Guid, List<DirectoryEntity>> children,
        IDictionary<Guid, string> pathMap)
    {
        pathMap[current.Id] = path;

        if (!children.TryGetValue(current.Id, out List<DirectoryEntity>? childDirectories))
        {
            return;
        }

        foreach (DirectoryEntity child in childDirectories.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            BuildDirectoryPathMapRecursively(child, $"{path}/{child.Name}", children, pathMap);
        }
    }

    private Dictionary<Guid, string> BuildFilePathMap(Dictionary<Guid, string> directoryPathMap)
    {
        List<FileEntity> files = _dbContext.Files
            .AsNoTracking()
            .Select(item => new FileEntity
            {
                Id = item.Id,
                DirectoryId = item.DirectoryId,
                Name = item.Name
            })
            .ToList();

        Dictionary<Guid, string> filePathMap = new();
        foreach (FileEntity file in files)
        {
            if (!directoryPathMap.TryGetValue(file.DirectoryId, out string? parentPath))
            {
                continue;
            }

            filePathMap[file.Id] = $"{parentPath}/{file.Name}";
        }

        return filePathMap;
    }

    private static string? ResolveNodePath(NodeTagEntity nodeTag, Dictionary<Guid, string> directoryPathMap, Dictionary<Guid, string> filePathMap)
    {
        if (nodeTag.DirectoryId.HasValue && directoryPathMap.TryGetValue(nodeTag.DirectoryId.Value, out string? directoryPath))
        {
            return directoryPath;
        }

        if (nodeTag.FileId.HasValue && filePathMap.TryGetValue(nodeTag.FileId.Value, out string? filePath))
        {
            return filePath;
        }

        return null;
    }

    private static bool IsPathInScope(string nodePath, string scopePath)
    {
        return nodePath.Equals(scopePath, StringComparison.OrdinalIgnoreCase)
            || nodePath.StartsWith($"{scopePath}/", StringComparison.OrdinalIgnoreCase);
    }
}
