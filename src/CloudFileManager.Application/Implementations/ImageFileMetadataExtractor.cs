using CloudFileManager.Application.Models;

namespace CloudFileManager.Application.Implementations;

public sealed class ImageFileMetadataExtractor : IFileMetadataExtractor
{
    private static readonly HashSet<string> SupportedExtensions =
    [
        ".png",
        ".jpg",
        ".jpeg"
    ];

    public bool CanHandle(string extension)
    {
        return SupportedExtensions.Contains(extension);
    }

    public UploadFileRequest BuildRequest(string targetDirectoryPath, string fileName, long fileSize, string localPath)
    {
        (int width, int height) = ReadImageSize(localPath);
        return new UploadFileRequest(
            targetDirectoryPath,
            fileName,
            fileSize,
            Width: width,
            Height: height,
            SourceLocalPath: localPath);
    }

    private static (int Width, int Height) ReadImageSize(string localPath)
    {
        string extension = Path.GetExtension(localPath).ToLowerInvariant();
        return extension switch
        {
            ".png" => ReadPngSize(localPath),
            ".jpg" or ".jpeg" => ReadJpegSize(localPath),
            _ => throw new InvalidOperationException($"Unsupported image type: {extension}")
        };
    }

    private static (int Width, int Height) ReadPngSize(string localPath)
    {
        using FileStream stream = File.OpenRead(localPath);
        using BinaryReader reader = new(stream);

        byte[] signature = reader.ReadBytes(8);
        byte[] expected = [137, 80, 78, 71, 13, 10, 26, 10];
        if (signature.Length != 8 || !signature.SequenceEqual(expected))
        {
            throw new InvalidOperationException("Invalid PNG file.");
        }

        _ = ReadUInt32BigEndian(reader);
        string chunkType = new string(reader.ReadChars(4));
        if (!string.Equals(chunkType, "IHDR", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid PNG header.");
        }

        int width = (int)ReadUInt32BigEndian(reader);
        int height = (int)ReadUInt32BigEndian(reader);
        return (width, height);
    }

    private static (int Width, int Height) ReadJpegSize(string localPath)
    {
        using FileStream stream = File.OpenRead(localPath);
        using BinaryReader reader = new(stream);

        byte first = reader.ReadByte();
        byte second = reader.ReadByte();
        if (first != 0xFF || second != 0xD8)
        {
            throw new InvalidOperationException("Invalid JPEG file.");
        }

        while (stream.Position < stream.Length)
        {
            byte markerStart = reader.ReadByte();
            if (markerStart != 0xFF)
            {
                continue;
            }

            byte marker = reader.ReadByte();
            while (marker == 0xFF)
            {
                marker = reader.ReadByte();
            }

            if (marker is 0xD8 or 0xD9)
            {
                continue;
            }

            ushort segmentLength = ReadUInt16BigEndian(reader);
            if (segmentLength < 2)
            {
                throw new InvalidOperationException("Invalid JPEG segment.");
            }

            if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF)
            {
                _ = reader.ReadByte();
                int height = ReadUInt16BigEndian(reader);
                int width = ReadUInt16BigEndian(reader);
                return (width, height);
            }

            stream.Seek(segmentLength - 2, SeekOrigin.Current);
        }

        throw new InvalidOperationException("Cannot read JPEG dimensions.");
    }

    private static ushort ReadUInt16BigEndian(BinaryReader reader)
    {
        byte high = reader.ReadByte();
        byte low = reader.ReadByte();
        return (ushort)((high << 8) | low);
    }

    private static uint ReadUInt32BigEndian(BinaryReader reader)
    {
        byte b1 = reader.ReadByte();
        byte b2 = reader.ReadByte();
        byte b3 = reader.ReadByte();
        byte b4 = reader.ReadByte();
        return (uint)((b1 << 24) | (b2 << 16) | (b3 << 8) | b4);
    }
}
