namespace CloudFileManager.Infrastructure.FileStorage;

/// <summary>
/// AuditTrailWriter 類別，負責輸出操作稽核紀錄。
/// </summary>
public sealed class AuditTrailWriter
{
    private readonly bool _enabled;
    private readonly string _auditLogPath;

    /// <summary>
    /// 初始化 AuditTrailWriter。
    /// </summary>
    public AuditTrailWriter(bool enabled, string storageRootPath)
    {
        _enabled = enabled;
        _auditLogPath = Path.Combine(storageRootPath, "audit.log");
    }

    /// <summary>
    /// 寫入稽核紀錄。
    /// </summary>
    public void Write(string line)
    {
        if (!_enabled)
        {
            return;
        }

        string payload = $"{DateTime.UtcNow:O}|{line}";
        File.AppendAllLines(_auditLogPath, [payload]);
    }
}
