using CloudFileManager.Application.Models;
using CloudFileManager.Domain;
using CloudFileManager.Domain.Enums;
using CloudFileManager.Infrastructure.DataAccess.EfCore.Entities;
using CloudFileManager.Infrastructure.FileStorage;

namespace CloudFileManager.UnitTests;

public sealed class StorageFileMetadataMapperTests
{
    [Fact]
    public void CreateMetadata_ShouldMapWordMetadata_WithDefaultPageCount()
    {
        Guid fileId = Guid.NewGuid();
        UploadFileRequest request = new("Root", "a.docx", 10);

        FileMetadataEntity metadata = StorageFileMetadataMapper.CreateMetadata(fileId, CloudFileType.Word, request);

        Assert.Equal(fileId, metadata.FileId);
        Assert.Equal(1, metadata.FileType);
        Assert.Equal(1, metadata.PageCount);
        Assert.Null(metadata.Width);
        Assert.Null(metadata.Height);
        Assert.Null(metadata.Encoding);
    }

    [Fact]
    public void CreateMetadata_ShouldMapImageMetadata_WithDefaultDimensions()
    {
        Guid fileId = Guid.NewGuid();
        UploadFileRequest request = new("Root", "a.png", 20);

        FileMetadataEntity metadata = StorageFileMetadataMapper.CreateMetadata(fileId, CloudFileType.Image, request);

        Assert.Equal(fileId, metadata.FileId);
        Assert.Equal(2, metadata.FileType);
        Assert.Null(metadata.PageCount);
        Assert.Equal(1920, metadata.Width);
        Assert.Equal(1080, metadata.Height);
        Assert.Null(metadata.Encoding);
    }

    [Fact]
    public void CreateMetadata_ShouldMapTextMetadata_WithTrimmedEncoding()
    {
        Guid fileId = Guid.NewGuid();
        UploadFileRequest request = new("Root", "a.txt", 30, Encoding: "  Big5  ");

        FileMetadataEntity metadata = StorageFileMetadataMapper.CreateMetadata(fileId, CloudFileType.Text, request);

        Assert.Equal(fileId, metadata.FileId);
        Assert.Equal(3, metadata.FileType);
        Assert.Null(metadata.PageCount);
        Assert.Null(metadata.Width);
        Assert.Null(metadata.Height);
        Assert.Equal("Big5", metadata.Encoding);
    }

    [Fact]
    public void CreateMetadata_ShouldThrow_WhenFileTypeIsUnsupported()
    {
        UploadFileRequest request = new("Root", "a.bin", 1);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            StorageFileMetadataMapper.CreateMetadata(Guid.NewGuid(), (CloudFileType)99, request));

        Assert.Equal("Unsupported file type: 99", exception.Message);
    }

    [Fact]
    public void BuildFile_ShouldMapAllSupportedFileTypes_AndFallback()
    {
        DateTime now = DateTime.UtcNow;

        CloudFile word = StorageFileMetadataMapper.BuildFile(new FileEntity
        {
            Name = "a.docx",
            SizeBytes = 10,
            CreatedTime = now,
            FileType = 1,
            Metadata = new FileMetadataEntity { PageCount = 7 }
        });

        CloudFile image = StorageFileMetadataMapper.BuildFile(new FileEntity
        {
            Name = "a.png",
            SizeBytes = 20,
            CreatedTime = now,
            FileType = 2,
            Metadata = new FileMetadataEntity { Width = 300, Height = 200 }
        });

        CloudFile text = StorageFileMetadataMapper.BuildFile(new FileEntity
        {
            Name = "a.txt",
            SizeBytes = 30,
            CreatedTime = now,
            FileType = 3,
            Metadata = new FileMetadataEntity { Encoding = "UTF-16" }
        });

        CloudFile fallback = StorageFileMetadataMapper.BuildFile(new FileEntity
        {
            Name = "a.bin",
            SizeBytes = 40,
            CreatedTime = now,
            FileType = 99,
            Metadata = null!
        });

        Assert.IsType<WordFile>(word);
        Assert.Equal("PageCount=7", word.DetailText);

        Assert.IsType<ImageFile>(image);
        Assert.Equal("Resolution=300x200", image.DetailText);

        Assert.IsType<TextFile>(text);
        Assert.Equal("Encoding=UTF-16", text.DetailText);

        Assert.IsType<TextFile>(fallback);
        Assert.Equal("Encoding=UTF-8", fallback.DetailText);
    }

    [Fact]
    public void BuildFile_ShouldUseDefaultImageDimensions_WhenMetadataIsMissing()
    {
        DateTime now = DateTime.UtcNow;

        CloudFile image = StorageFileMetadataMapper.BuildFile(new FileEntity
        {
            Name = "a.png",
            SizeBytes = 20,
            CreatedTime = now,
            FileType = 2,
            Metadata = null!
        });

        Assert.IsType<ImageFile>(image);
        Assert.Equal("Resolution=1920x1080", image.DetailText);
    }
}
