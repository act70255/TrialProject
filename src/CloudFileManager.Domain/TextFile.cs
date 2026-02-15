using CloudFileManager.Domain.Enums;

namespace CloudFileManager.Domain;

/// <summary>
/// TextFile，封裝文字檔案專屬資訊。
/// </summary>
public sealed class TextFile : CloudFile
{
    /// <summary>
    /// 初始化 TextFile。
    /// </summary>
    public TextFile(string name, long size, DateTime createdTime, string encoding)
        : base(name, size, createdTime, CloudFileType.Text)
    {
        if (string.IsNullOrWhiteSpace(encoding))
        {
            throw new ArgumentException("Encoding is required.", nameof(encoding));
        }

        Encoding = encoding.Trim();
    }

    /// <summary>
    /// 取得文字編碼。
    /// </summary>
    public string Encoding { get; }

    public override string DetailText => $"Encoding={Encoding}";
}
