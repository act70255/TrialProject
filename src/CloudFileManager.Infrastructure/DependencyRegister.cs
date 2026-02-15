using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Interfaces;
using CloudFileManager.Infrastructure.DataAccess.EfCore;
using CloudFileManager.Infrastructure.FileStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CloudFileManager.Infrastructure;

/// <summary>
/// DependencyRegister 類別，負責組態組裝與相依性註冊。
/// </summary>
public static class DependencyRegister
{
    /// <summary>
    /// 註冊資料。
    /// </summary>
    public static void Register(IServiceCollection services, AppConfig config, string basePath)
    {
        AddDataAccess(services, config, basePath);
        AddFileStorage(services);
    }

    /// <summary>
    /// 處理初始化作業。
    /// </summary>
    public static void Initialize(IServiceProvider serviceProvider, bool shouldMigrate)
    {
        InitializeAsync(serviceProvider, shouldMigrate).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 以非同步方式處理初始化作業。
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider serviceProvider, bool shouldMigrate, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        CloudFileDbContext dbContext = scope.ServiceProvider.GetRequiredService<CloudFileDbContext>();
        AppConfig config = scope.ServiceProvider.GetRequiredService<AppConfig>();
        string storageRootPath = StorageBootstrapper.ResolveStorageRootPath(config.Storage.StorageRootPath, AppContext.BaseDirectory);

        if (shouldMigrate)
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        EnsureSchemaReady(dbContext, shouldMigrate);
        await StorageMetadataInitializer.InitializeAsync(dbContext, storageRootPath, cancellationToken);
    }

    /// <summary>
    /// 新增資料存取層設定。
    /// </summary>
    private static void AddDataAccess(IServiceCollection services, AppConfig config, string basePath)
    {
        string provider = config.Database.Provider;

        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<CloudFileDbContext>(options =>
                options.UseSqlServer(config.Database.ConnectionStrings.SqlServer));
            return;
        }

        string sqliteConnection = ResolveSqliteConnectionString(config.Database.ConnectionStrings.Sqlite, basePath);
        services.AddDbContext<CloudFileDbContext>(options =>
            options.UseSqlite(sqliteConnection));
    }

    /// <summary>
    /// 新增檔案儲存服務。
    /// </summary>
    private static void AddFileStorage(IServiceCollection services)
    {
        services.AddScoped<IStorageMetadataGateway, StorageMetadataGateway>();
        services.AddScoped<IXmlOutputWriter, FileSystemXmlOutputWriter>();
    }

    /// <summary>
    /// 解析 Sqlite 連線字串。
    /// </summary>
    private static string ResolveSqliteConnectionString(string connectionString, string basePath)
    {
        const string dataSourcePrefix = "Data Source=";
        if (!connectionString.StartsWith(dataSourcePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        string rawPath = connectionString[dataSourcePrefix.Length..].Trim();
        if (Path.IsPathRooted(rawPath))
        {
            return connectionString;
        }

        string fullPath = Path.GetFullPath(Path.Combine(basePath, rawPath));
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return $"{dataSourcePrefix}{fullPath}";
    }

    /// <summary>
    /// 確保資料庫結構可用。
    /// </summary>
    private static void EnsureSchemaReady(CloudFileDbContext dbContext, bool migrationEnabled)
    {
        try
        {
            _ = dbContext.Directories.Any();
        }
        catch (Exception ex)
        {
            string message = migrationEnabled
                ? "Database schema check failed after migration. Please inspect migration state and connection string."
                : "Database schema is not ready. Set Database.MigrateOnStartup=true or run 'dotnet ef database update' before starting the service.";

            throw new InvalidOperationException(message, ex);
        }
    }
}
