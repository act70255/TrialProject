using CloudFileManager.Domain.Enums;

namespace CloudFileManager.Domain;

/// <summary>
/// WordFile，封裝 Word 檔案專屬資訊。
/// </summary>
public sealed class WordFile : CloudFile
{
    /// <summary>
    /// 初始化 WordFile。
    /// </summary>
    public WordFile(string name, long size, DateTime createdTime, int pageCount)
        : base(name, size, createdTime, CloudFileType.Word)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageCount);

        PageCount = pageCount;
    }

    /// <summary>
    /// 取得頁數。
    /// </summary>
    public int PageCount { get; }

    public override string DetailText => $"PageCount={PageCount}";
}
