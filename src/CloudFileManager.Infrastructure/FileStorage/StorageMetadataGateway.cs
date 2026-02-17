using CloudFileManager.Application.Interfaces;
using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Models;
using CloudFileManager.Domain;
using CloudFileManager.Domain.Enums;
using CloudFileManager.Infrastructure.DataAccess.EfCore;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using CloudFileManager.Shared.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CloudFileManager.Infrastructure.FileStorage;

/// <summary>
/// StorageMetadataGateway 類別，負責資料存取與持久化操作。
/// </summary>
public sealed partial class StorageMetadataGateway : IStorageMetadataGateway
{
    private static readonly MemoryCache FallbackTreeCache = new(new MemoryCacheOptions());
    private const string RootTreeCacheKey = "cfm:root-tree";

    private readonly CloudFileDbContext _dbContext;
    private readonly string _storageRootPath;
    private readonly ManagementConfig _management;
    private readonly AuditTrailWriter _auditTrailWriter;
    private readonly IMemoryCache _treeCache;
    private readonly ILogger<StorageMetadataGateway> _logger;

    /// <summary>
    /// 初始化 StorageMetadataGateway。
    /// </summary>
    public StorageMetadataGateway(CloudFileDbContext dbContext, AppConfig config, IMemoryCache? treeCache = null, ILogger<StorageMetadataGateway>? logger = null)
    {
        _dbContext = dbContext;
        _storageRootPath = ResolveStorageRootPath(config.Storage.StorageRootPath);
        _management = config.Management;
        _auditTrailWriter = new AuditTrailWriter(_management.EnableAuditLog, _storageRootPath);
        _treeCache = treeCache ?? FallbackTreeCache;
        _logger = logger ?? NullLogger<StorageMetadataGateway>.Instance;
    }

    /// <summary>
    /// 載入根目錄樹狀結構。
    /// </summary>
    public CloudDirectory LoadRootTree()
    {
        if (_treeCache.TryGetValue(GetRootTreeCacheKey(), out CloudDirectory? cachedRoot) && cachedRoot is not null)
        {
            return CloneDirectoryTree(cachedRoot);
        }

        List<DirectoryEntity> directories = _dbContext.Directories
            .AsNoTracking()
            .OrderBy(item => item.CreationOrder)
            .ToList();

        List<FileEntity> files = _dbContext.Files
            .AsNoTracking()
            .Include(item => item.Metadata)
            .OrderBy(item => item.CreationOrder)
            .ToList();

        DirectoryEntity? rootEntity = directories.FirstOrDefault(item => item.ParentId == null && item.Name == "Root");
        if (rootEntity is null)
        {
            CloudDirectory root = new("Root", DateTime.UtcNow);
            _treeCache.Set(GetRootTreeCacheKey(), CloneDirectoryTree(root), TimeSpan.FromSeconds(30));
            return root;
        }

        Dictionary<Guid, CloudDirectory> directoryMap = directories.ToDictionary(
            item => item.Id,
            item => new CloudDirectory(item.Name, item.CreatedTime));

        foreach (DirectoryEntity directory in directories.Where(item => item.ParentId is not null))
        {
            Guid parentId = directory.ParentId!.Value;
            if (directoryMap.TryGetValue(parentId, out CloudDirectory? parentDirectory) &&
                directoryMap.TryGetValue(directory.Id, out CloudDirectory? childDirectory))
            {
                parentDirectory.AttachDirectory(childDirectory);
            }
        }

        foreach (FileEntity fileEntity in files)
        {
            if (!directoryMap.TryGetValue(fileEntity.DirectoryId, out CloudDirectory? parentDirectory))
            {
                continue;
            }

            CloudFile file = StorageFileMetadataMapper.BuildFile(fileEntity);
            parentDirectory.AddFile(file);
        }

        CloudDirectory loadedRoot = directoryMap[rootEntity.Id];
        _treeCache.Set(GetRootTreeCacheKey(), CloneDirectoryTree(loadedRoot), TimeSpan.FromSeconds(30));
        return loadedRoot;
    }

    /// <summary>
    /// 解析Storage根目錄路徑。
    /// </summary>
    private static string ResolveStorageRootPath(string storageRootPath)
    {
        return StorageBootstrapper.ResolveStorageRootPath(storageRootPath, AppContext.BaseDirectory);
    }

    private string GetRootTreeCacheKey()
    {
        return $"{RootTreeCacheKey}:{_storageRootPath}";
    }

    private void InvalidateRootTreeCache()
    {
        _treeCache.Remove(GetRootTreeCacheKey());
    }

    private static CloudDirectory CloneDirectoryTree(CloudDirectory source)
    {
        CloudDirectory clone = new(source.Name, source.CreatedTime);

        foreach (CloudFile file in source.Files)
        {
            clone.AddFile(CloneFile(file));
        }

        foreach (CloudDirectory childDirectory in source.Directories)
        {
            clone.AttachDirectory(CloneDirectoryTree(childDirectory));
        }

        return clone;
    }

    private static CloudFile CloneFile(CloudFile source)
    {
        return source switch
        {
            WordFile wordFile => new WordFile(wordFile.Name, wordFile.Size, wordFile.CreatedTime, wordFile.PageCount),
            ImageFile imageFile => new ImageFile(imageFile.Name, imageFile.Size, imageFile.CreatedTime, imageFile.Width, imageFile.Height),
            TextFile textFile => new TextFile(textFile.Name, textFile.Size, textFile.CreatedTime, textFile.Encoding),
            _ => throw new InvalidOperationException($"Unsupported file type: {source.GetType().Name}")
        };
    }

    private string ResolvePhysicalPath(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return _storageRootPath;
        }

        if (Path.IsPathRooted(storedPath))
        {
            return Path.GetFullPath(storedPath);
        }

        string normalized = storedPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(_storageRootPath, normalized));
    }

    private string ToStoredPath(string physicalPath)
    {
        string fullPath = Path.GetFullPath(physicalPath);
        string normalizedRoot = _storageRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string rootWithSeparator = normalizedRoot + Path.DirectorySeparatorChar;

        if (string.Equals(fullPath, normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath[rootWithSeparator.Length..].Replace('\\', '/');
        }

        return fullPath.Replace('\\', '/');
    }

}
