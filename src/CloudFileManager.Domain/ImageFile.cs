using CloudFileManager.Domain.Enums;

namespace CloudFileManager.Domain;

/// <summary>
/// ImageFile，封裝影像檔案專屬資訊。
/// </summary>
public sealed class ImageFile : CloudFile
{
    /// <summary>
    /// 初始化 ImageFile。
    /// </summary>
    public ImageFile(string name, long size, DateTime createdTime, int width, int height)
        : base(name, size, createdTime, CloudFileType.Image)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
    }

    /// <summary>
    /// 取得寬度。
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// 取得高度。
    /// </summary>
    public int Height { get; }

    public override string DetailText => $"Resolution={Width}x{Height}";
}
