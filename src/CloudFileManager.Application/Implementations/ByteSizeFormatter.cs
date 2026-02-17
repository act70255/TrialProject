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
        if (bytes == 0)
        {
            return "0KB";
        }

        return $"{bytes / 1024d:0.###}KB";
    }
}
