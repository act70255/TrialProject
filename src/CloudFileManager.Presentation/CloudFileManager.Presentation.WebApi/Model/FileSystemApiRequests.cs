using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CloudFileManager.Presentation.WebApi.Model;

/// <summary>
/// 建立目錄API請求 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class CreateDirectoryApiRequest
{
    public CreateDirectoryApiRequest()
    {
        ParentPath = string.Empty;
        DirectoryName = string.Empty;
    }

    public CreateDirectoryApiRequest(string parentPath, string directoryName)
    {
        ParentPath = parentPath;
        DirectoryName = directoryName;
    }

    [Required]
    [MinLength(1)]
    public string ParentPath { get; set; }

    [Required]
    [MinLength(1)]
    public string DirectoryName { get; set; }
}

/// <summary>
/// 上傳檔案API請求 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class UploadFileApiRequest
{
    public UploadFileApiRequest()
    {
        DirectoryPath = string.Empty;
        FileName = string.Empty;
    }

    public UploadFileApiRequest(
        string directoryPath,
        string fileName,
        long size,
        int? pageCount,
        int? width,
        int? height,
        string? encoding)
    {
        DirectoryPath = directoryPath;
        FileName = fileName;
        Size = size;
        PageCount = pageCount;
        Width = width;
        Height = height;
        Encoding = encoding;
    }

    [Required]
    [MinLength(1)]
    public string DirectoryPath { get; set; }

    [Required]
    [MinLength(1)]
    public string FileName { get; set; }

    [Range(0, long.MaxValue)]
    public long Size { get; set; }

    public int? PageCount { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public string? Encoding { get; set; }
}

/// <summary>
/// 上傳檔案FormAPI請求 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class UploadFileFormApiRequest
{
    public UploadFileFormApiRequest()
    {
        DirectoryPath = "Root";
    }

    [Required]
    [MinLength(1)]
    public string DirectoryPath { get; set; }

    public IFormFile? File { get; set; }
}

/// <summary>
/// 搬移檔案API請求 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class MoveFileApiRequest
{
    public MoveFileApiRequest()
    {
        SourceFilePath = string.Empty;
        TargetDirectoryPath = string.Empty;
    }

    public MoveFileApiRequest(string sourceFilePath, string targetDirectoryPath)
    {
        SourceFilePath = sourceFilePath;
        TargetDirectoryPath = targetDirectoryPath;
    }

    [Required]
    [MinLength(1)]
    public string SourceFilePath { get; set; }

    [Required]
    [MinLength(1)]
    public string TargetDirectoryPath { get; set; }
}

/// <summary>
/// 重新命名檔案API請求 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class RenameFileApiRequest
{
    public RenameFileApiRequest()
    {
        FilePath = string.Empty;
        NewFileName = string.Empty;
    }

    public RenameFileApiRequest(string filePath, string newFileName)
    {
        FilePath = filePath;
        NewFileName = newFileName;
    }

    [Required]
    [MinLength(1)]
    public string FilePath { get; set; }

    [Required]
    [MinLength(1)]
    public string NewFileName { get; set; }
}

/// <summary>
/// 刪除檔案API請求 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class DeleteFileApiRequest
{
    public DeleteFileApiRequest()
    {
        FilePath = string.Empty;
    }

    public DeleteFileApiRequest(string filePath)
    {
        FilePath = filePath;
    }

    [Required]
    [MinLength(1)]
    public string FilePath { get; set; }
}

/// <summary>
/// 刪除目錄API請求 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class DeleteDirectoryApiRequest
{
    public DeleteDirectoryApiRequest()
    {
        DirectoryPath = string.Empty;
    }

    public DeleteDirectoryApiRequest(string directoryPath)
    {
        DirectoryPath = directoryPath;
    }

    [Required]
    [MinLength(1)]
    public string DirectoryPath { get; set; }
}

/// <summary>
/// 搬移目錄API請求 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class MoveDirectoryApiRequest
{
    public MoveDirectoryApiRequest()
    {
        SourceDirectoryPath = string.Empty;
        TargetParentDirectoryPath = string.Empty;
    }

    public MoveDirectoryApiRequest(string sourceDirectoryPath, string targetParentDirectoryPath)
    {
        SourceDirectoryPath = sourceDirectoryPath;
        TargetParentDirectoryPath = targetParentDirectoryPath;
    }

    [Required]
    [MinLength(1)]
    public string SourceDirectoryPath { get; set; }

    [Required]
    [MinLength(1)]
    public string TargetParentDirectoryPath { get; set; }
}

/// <summary>
/// 重新命名目錄API請求 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class RenameDirectoryApiRequest
{
    public RenameDirectoryApiRequest()
    {
        DirectoryPath = string.Empty;
        NewDirectoryName = string.Empty;
    }

    public RenameDirectoryApiRequest(string directoryPath, string newDirectoryName)
    {
        DirectoryPath = directoryPath;
        NewDirectoryName = newDirectoryName;
    }

    [Required]
    [MinLength(1)]
    public string DirectoryPath { get; set; }

    [Required]
    [MinLength(1)]
    public string NewDirectoryName { get; set; }
}

/// <summary>
/// 計算容量API請求 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class CalculateSizeApiRequest
{
    public CalculateSizeApiRequest()
    {
        Path = string.Empty;
    }

    public CalculateSizeApiRequest(string path)
    {
        Path = path;
    }

    [Required]
    [MinLength(1)]
    public string Path { get; set; }
}

/// <summary>
/// 副檔名搜尋 API 請求類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class SearchByExtensionApiRequest
{
    public SearchByExtensionApiRequest()
    {
        Extension = string.Empty;
    }

    public SearchByExtensionApiRequest(string extension)
    {
        Extension = extension;
    }

    [Required]
    [MinLength(1)]
    public string Extension { get; set; }
}
