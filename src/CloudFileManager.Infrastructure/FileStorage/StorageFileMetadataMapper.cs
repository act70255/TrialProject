using CloudFileManager.Application.Models;
using CloudFileManager.Domain;
using CloudFileManager.Domain.Enums;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;

namespace CloudFileManager.Infrastructure.FileStorage;

/// <summary>
/// StorageFileMetadataMapper 類別，負責檔案與中繼資料轉換。
/// </summary>
public static class StorageFileMetadataMapper
{
    private const int DefaultWordPageCount = 1;
    private const int DefaultImageWidth = 1920;
    private const int DefaultImageHeight = 1080;
    private const string DefaultTextEncoding = "UTF-8";

    /// <summary>
    /// 建立中繼資料實體。
    /// </summary>
    public static FileMetadataEntity CreateMetadata(Guid fileId, CloudFileType fileType, UploadFileRequest request)
    {
        return fileType switch
        {
            CloudFileType.Word => new FileMetadataEntity
            {
                FileId = fileId,
                FileType = (int)fileType,
                PageCount = request.PageCount.GetValueOrDefault(DefaultWordPageCount),
                Width = null,
                Height = null,
                Encoding = null
            },
            CloudFileType.Image => new FileMetadataEntity
            {
                FileId = fileId,
                FileType = (int)fileType,
                PageCount = null,
                Width = request.Width.GetValueOrDefault(DefaultImageWidth),
                Height = request.Height.GetValueOrDefault(DefaultImageHeight),
                Encoding = null
            },
            CloudFileType.Text => new FileMetadataEntity
            {
                FileId = fileId,
                FileType = (int)fileType,
                PageCount = null,
                Width = null,
                Height = null,
                Encoding = string.IsNullOrWhiteSpace(request.Encoding) ? DefaultTextEncoding : request.Encoding.Trim()
            },
            _ => throw new InvalidOperationException($"Unsupported file type: {fileType}")
        };
    }

    /// <summary>
    /// 建立 Domain 檔案物件。
    /// </summary>
    public static CloudFile BuildFile(FileEntity fileEntity)
    {
        int fileType = fileEntity.FileType;
        FileMetadataEntity? metadata = fileEntity.Metadata;

        return fileType switch
        {
            1 => new WordFile(fileEntity.Name, fileEntity.SizeBytes, fileEntity.CreatedTime, metadata?.PageCount ?? DefaultWordPageCount),
            2 => new ImageFile(fileEntity.Name, fileEntity.SizeBytes, fileEntity.CreatedTime, metadata?.Width ?? DefaultImageWidth, metadata?.Height ?? DefaultImageHeight),
            3 => new TextFile(fileEntity.Name, fileEntity.SizeBytes, fileEntity.CreatedTime, string.IsNullOrWhiteSpace(metadata?.Encoding) ? DefaultTextEncoding : metadata!.Encoding!),
            _ => new TextFile(fileEntity.Name, fileEntity.SizeBytes, fileEntity.CreatedTime, DefaultTextEncoding)
        };
    }
}
