using CloudFileManager.Application.Configuration;
using CloudFileManager.Domain;
using CloudFileManager.Infrastructure;
using CloudFileManager.Infrastructure.DataAccess.EfCore;
using Microsoft.Extensions.DependencyInjection;

namespace CloudFileManager.IntegrationTests;

/// <summary>
/// ProviderCompatibilityTests 類別，負責封裝該領域的核心資料與行為。
/// </summary>
public class ProviderCompatibilityTests
{
    [Fact]
    public void Should_UseSqliteByDefault_AndAllowSqlServerSwitch()
    {
        string basePath = Path.Combine(Path.GetTempPath(), $"cfm-provider-{Guid.NewGuid():N}");
        Directory.CreateDirectory(basePath);

        AppConfig sqliteConfig = ConfigDefaults.ApplyDefaults(new AppConfig());
        sqliteConfig.Database.ConnectionStrings.Sqlite = $"Data Source={Path.Combine(basePath, "default.db")}";

        ServiceCollection sqliteServices = new();
        DependencyRegister.Register(sqliteServices, sqliteConfig, basePath);
        using ServiceProvider sqliteProvider = sqliteServices.BuildServiceProvider();
        using IServiceScope sqliteScope = sqliteProvider.CreateScope();
        CloudFileDbContext sqliteContext = sqliteScope.ServiceProvider.GetRequiredService<CloudFileDbContext>();

        Assert.Contains("Sqlite", sqliteContext.Database.ProviderName ?? string.Empty, StringComparison.Ordinal);

        AppConfig sqlServerConfig = ConfigDefaults.ApplyDefaults(new AppConfig());
        sqlServerConfig.Database.Provider = "SqlServer";
        sqlServerConfig.Database.ConnectionStrings.SqlServer = "Server=localhost;Database=CloudFileManager_Test;User Id=sa;Password=Your_password123;TrustServerCertificate=true";

        ServiceCollection sqlServerServices = new();
        DependencyRegister.Register(sqlServerServices, sqlServerConfig, basePath);
        using ServiceProvider sqlServerProvider = sqlServerServices.BuildServiceProvider();
        using IServiceScope sqlServerScope = sqlServerProvider.CreateScope();
        CloudFileDbContext sqlServerContext = sqlServerScope.ServiceProvider.GetRequiredService<CloudFileDbContext>();

        Assert.Contains("SqlServer", sqlServerContext.Database.ProviderName ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_KeepDomainContractStable_WhenProviderSwitches()
    {
        AppConfig sqliteConfig = ConfigDefaults.ApplyDefaults(new AppConfig());
        AppConfig sqlServerConfig = ConfigDefaults.ApplyDefaults(new AppConfig
        {
            Database = new DatabaseConfig
            {
                Provider = "SqlServer",
                ConnectionStrings = new DatabaseConnectionStringsConfig
                {
                    SqlServer = "Server=localhost;Database=CloudFileManager_Test;User Id=sa;Password=Your_password123;TrustServerCertificate=true"
                }
            }
        });

        CloudDirectory root = new("Root", DateTime.UtcNow);
        CloudDirectory docs = root.AddDirectory("Docs", DateTime.UtcNow);
        docs.AddFile(new WordFile("Spec.docx", 1024, DateTime.UtcNow, 2));

        long size = root.CalculateTotalBytes();

        Assert.Equal(1024, size);
        Assert.Equal("Sqlite", sqliteConfig.Database.Provider);
        Assert.Equal("SqlServer", sqlServerConfig.Database.Provider);

        string[] referencedAssemblies = typeof(CloudDirectory).Assembly.GetReferencedAssemblies().Select(item => item.Name ?? string.Empty).ToArray();
        Assert.DoesNotContain(referencedAssemblies, item => item.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }
}
