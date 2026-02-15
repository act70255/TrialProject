using CloudFileManager.Infrastructure.DataAccess.EfCore;

namespace CloudFileManager.Infrastructure.FileStorage;

public static class StorageDbProviderClassifier
{
    public static bool IsSqlite(CloudFileDbContext dbContext)
    {
        string? provider = dbContext.Database.ProviderName;
        return provider?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
    }

    public static bool IsSqlServer(CloudFileDbContext dbContext)
    {
        string? provider = dbContext.Database.ProviderName;
        return provider?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;
    }
}
