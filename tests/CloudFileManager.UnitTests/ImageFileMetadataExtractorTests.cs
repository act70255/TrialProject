using CloudFileManager.Application.Implementations;

namespace CloudFileManager.UnitTests;

public sealed class ImageFileMetadataExtractorTests
{
    private readonly ImageFileMetadataExtractor _extractor = new();

    [Theory]
    [InlineData(".png", true)]
    [InlineData(".jpg", true)]
    [InlineData(".jpeg", true)]
    [InlineData(".gif", false)]
    public void CanHandle_ShouldReturnExpectedResult(string extension, bool expected)
    {
        bool actual = _extractor.CanHandle(extension);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BuildRequest_ShouldReadJpegDimensions()
    {
        string path = CreateJpegFile(120, 45);

        try
        {
            var result = _extractor.BuildRequest("Root", "photo.jpg", 888, path);

            Assert.Equal("Root", result.DirectoryPath);
            Assert.Equal("photo.jpg", result.FileName);
            Assert.Equal(888, result.Size);
            Assert.Equal(120, result.Width);
            Assert.Equal(45, result.Height);
            Assert.Equal(path, result.SourceLocalPath);
            Assert.Null(result.PageCount);
            Assert.Null(result.Encoding);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void BuildRequest_ShouldThrow_WhenPngSignatureIsInvalid()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cfm-image-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, [0, 1, 2, 3, 4, 5, 6, 7]);

        try
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                _extractor.BuildRequest("Root", "bad.png", 8, path));

            Assert.Equal("Invalid PNG file.", exception.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void BuildRequest_ShouldThrow_WhenExtensionIsUnsupported()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cfm-image-{Guid.NewGuid():N}.gif");
        File.WriteAllBytes(path, [1, 2, 3]);

        try
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                _extractor.BuildRequest("Root", "bad.gif", 3, path));

            Assert.Equal("Unsupported image type: .gif", exception.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateJpegFile(int width, int height)
    {
        string path = Path.Combine(Path.GetTempPath(), $"cfm-image-{Guid.NewGuid():N}.jpg");
        using FileStream stream = File.Create(path);
        using BinaryWriter writer = new(stream);

        writer.Write((byte)0xFF);
        writer.Write((byte)0xD8);

        writer.Write((byte)0xFF);
        writer.Write((byte)0xE0);
        WriteUInt16BigEndian(writer, 16);
        writer.Write(new byte[14]);

        writer.Write((byte)0xFF);
        writer.Write((byte)0xC0);
        WriteUInt16BigEndian(writer, 17);
        writer.Write((byte)8);
        WriteUInt16BigEndian(writer, (ushort)height);
        WriteUInt16BigEndian(writer, (ushort)width);
        writer.Write((byte)3);
        writer.Write((byte)1);
        writer.Write((byte)0x11);
        writer.Write((byte)0);
        writer.Write((byte)2);
        writer.Write((byte)0x11);
        writer.Write((byte)1);
        writer.Write((byte)3);
        writer.Write((byte)0x11);
        writer.Write((byte)1);

        writer.Write((byte)0xFF);
        writer.Write((byte)0xD9);

        return path;
    }

    private static void WriteUInt16BigEndian(BinaryWriter writer, ushort value)
    {
        writer.Write((byte)((value >> 8) & 0xFF));
        writer.Write((byte)(value & 0xFF));
    }
}
