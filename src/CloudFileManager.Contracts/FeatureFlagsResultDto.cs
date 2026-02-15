namespace CloudFileManager.Contracts;

/// <summary>
/// 功能旗標結果DTO 類別，負責封裝資料傳輸結構。
/// </summary>
public sealed class FeatureFlagsResultDto
{
    /// <summary>
    /// 初始化 功能旗標結果DTO。
    /// </summary>
    public FeatureFlagsResultDto()
    {
        Flags = new Dictionary<string, bool>();
    }

    /// <summary>
    /// 初始化 功能旗標結果DTO。
    /// </summary>
    public FeatureFlagsResultDto(IReadOnlyDictionary<string, bool> flags)
    {
        Flags = flags;
    }

    public IReadOnlyDictionary<string, bool> Flags { get; set; }
}
