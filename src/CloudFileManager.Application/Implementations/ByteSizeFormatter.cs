namespace CloudFileManager.Application.Implementations;

/// <summary>
/// ByteSizeFormatter，提供位元組大小格式化。
/// </summary>
public static class ByteSizeFormatter
{
    /// <summary>
    /// 格式化檔案大小顯示文字。
    /// </summary>
    public static string Format(long bytes)
    {
        const double kilobyte = 1024d;
        const double megabyte = 1024d * 1024d;

        if (bytes >= megabyte)
        {
            return $"{bytes / megabyte:0.00} MB";
        }

        if (bytes >= kilobyte)
        {
            return $"{bytes / kilobyte:0.00} KB";
        }

        return $"{bytes} Bytes";
    }
}
