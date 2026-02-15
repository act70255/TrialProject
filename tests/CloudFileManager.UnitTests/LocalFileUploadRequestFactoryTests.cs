using System.IO.Compression;
using CloudFileManager.Application.Implementations;

namespace CloudFileManager.UnitTests;

/// <summary>
/// LocalFileUploadRequestFactoryTests 類別，負責驗證本機上傳請求建立流程。
/// </summary>
public sealed class LocalFileUploadRequestFactoryTests
{
    private readonly LocalFileUploadRequestFactory _factory = new([
        new WordFileMetadataExtractor(),
        new ImageFileMetadataExtractor(),
        new TextFileMetadataExtractor()
    ]);

    [Fact]
    public void Should_DetectUtf8Bom_ForTextFile()
    {
        string path = CreateTempFile(".txt", [0xEF, 0xBB, 0xBF, (byte)'a']);

        try
        {
            var uploadRequest = _factory.Create("Root", "sample.txt", path);

            Assert.Equal("sample.txt", uploadRequest.FileName);
            Assert.Equal("UTF-8", uploadRequest.Encoding);
            Assert.Null(uploadRequest.PageCount);
            Assert.Null(uploadRequest.Width);
            Assert.Null(uploadRequest.Height);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Should_ReadPngDimensions_ForImageFile()
    {
        string path = CreatePngFile(120, 45);

        try
        {
            var uploadRequest = _factory.Create("Root", "photo.png", path);

            Assert.Equal(120, uploadRequest.Width);
            Assert.Equal(45, uploadRequest.Height);
            Assert.Null(uploadRequest.Encoding);
            Assert.Null(uploadRequest.PageCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Should_ReadPageCount_ForDocxFile()
    {
        string path = CreateDocxWithPageCount(7);

        try
        {
            var uploadRequest = _factory.Create("Root", "doc.docx", path);

            Assert.Equal(7, uploadRequest.PageCount);
            Assert.Null(uploadRequest.Encoding);
            Assert.Null(uploadRequest.Width);
            Assert.Null(uploadRequest.Height);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateTempFile(string extension, byte[] content)
    {
        string path = Path.Combine(Path.GetTempPath(), $"cfm-test-{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(path, content);
        return path;
    }

    private static string CreatePngFile(int width, int height)
    {
        string path = Path.Combine(Path.GetTempPath(), $"cfm-test-{Guid.NewGuid():N}.png");
        using FileStream stream = File.Create(path);
        using BinaryWriter writer = new(stream);

        writer.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        WriteUInt32BigEndian(writer, 13);
        writer.Write(new[] { 'I', 'H', 'D', 'R' }.Select(ch => (byte)ch).ToArray());
        WriteUInt32BigEndian(writer, (uint)width);
        WriteUInt32BigEndian(writer, (uint)height);
        writer.Write(new byte[] { 8, 2, 0, 0, 0 });
        writer.Write(new byte[] { 0, 0, 0, 0 });

        return path;
    }

    private static string CreateDocxWithPageCount(int pageCount)
    {
        string path = Path.Combine(Path.GetTempPath(), $"cfm-test-{Guid.NewGuid():N}.docx");
        using ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create);
        ZipArchiveEntry entry = archive.CreateEntry("docProps/app.xml");
        using StreamWriter writer = new(entry.Open());
        writer.Write($"<Properties><Pages>{pageCount}</Pages></Properties>");
        return path;
    }

    private static void WriteUInt32BigEndian(BinaryWriter writer, uint value)
    {
        writer.Write((byte)((value >> 24) & 0xFF));
        writer.Write((byte)((value >> 16) & 0xFF));
        writer.Write((byte)((value >> 8) & 0xFF));
        writer.Write((byte)(value & 0xFF));
    }
}
