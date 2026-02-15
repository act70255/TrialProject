namespace CloudFileManager.Domain;

/// <summary>
/// CloudDirectory，封裝目錄節點與其子節點操作。
/// </summary>
public sealed class CloudDirectory : FileSystemNode
{
    private readonly List<CloudDirectory> _directories = new();
    private readonly List<CloudFile> _files = new();

    /// <summary>
    /// 初始化 CloudDirectory。
    /// </summary>
    public CloudDirectory(string name, DateTime createdTime)
        : base(name, createdTime)
    {
    }

    /// <summary>
    /// 取得子目錄集合。
    /// </summary>
    public IReadOnlyList<CloudDirectory> Directories => _directories;

    /// <summary>
    /// 取得檔案集合。
    /// </summary>
    public IReadOnlyList<CloudFile> Files => _files;

    /// <summary>
    /// 新增目錄。
    /// </summary>
    public CloudDirectory AddDirectory(string name, DateTime createdTime)
    {
        if (_directories.Any(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Directory already exists: {name}");
        }

        CloudDirectory directory = new(name.Trim(), createdTime);
        _directories.Add(directory);
        return directory;
    }

    /// <summary>
    /// 新增檔案。
    /// </summary>
    public void AddFile(CloudFile file)
    {
        if (_files.Any(item => item.Name.Equals(file.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"File already exists: {file.Name}");
        }

        _files.Add(file);
    }

    /// <summary>
    /// 移除檔案。
    /// </summary>
    public bool RemoveFile(string fileName)
    {
        CloudFile? file = _files.FirstOrDefault(item => item.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        if (file is null)
        {
            return false;
        }

        return _files.Remove(file);
    }

    /// <summary>
    /// 移除目錄。
    /// </summary>
    public bool RemoveDirectory(string directoryName)
    {
        CloudDirectory? directory = _directories.FirstOrDefault(item => item.Name.Equals(directoryName, StringComparison.OrdinalIgnoreCase));
        if (directory is null)
        {
            return false;
        }

        return _directories.Remove(directory);
    }

    /// <summary>
    /// 分離目錄。
    /// </summary>
    public CloudDirectory? DetachDirectory(string directoryName)
    {
        CloudDirectory? directory = _directories.FirstOrDefault(item => item.Name.Equals(directoryName, StringComparison.OrdinalIgnoreCase));
        if (directory is null)
        {
            return null;
        }

        _directories.Remove(directory);
        return directory;
    }

    /// <summary>
    /// 附加目錄。
    /// </summary>
    public void AttachDirectory(CloudDirectory directory)
    {
        if (_directories.Any(item => item.Name.Equals(directory.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Directory already exists: {directory.Name}");
        }

        _directories.Add(directory);
    }

    /// <summary>
    /// 計算目前目錄的總位元組數。
    /// </summary>
    public long CalculateTotalBytes(List<string>? traverseLog = null, string currentPath = "")
    {
        string path = string.IsNullOrWhiteSpace(currentPath) ? Name : currentPath;
        traverseLog?.Add($"[Directory] {path}");

        long bytes = 0;

        foreach (CloudFile file in _files)
        {
            traverseLog?.Add($"[File] {path}/{file.Name} ({file.GetType().Name})");
            bytes += file.Size;
        }

        foreach (CloudDirectory directory in _directories)
        {
            bytes += directory.CalculateTotalBytes(traverseLog, $"{path}/{directory.Name}");
        }

        return bytes;
    }
}
