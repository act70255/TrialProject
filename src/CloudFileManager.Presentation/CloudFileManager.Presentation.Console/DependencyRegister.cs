using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CloudFileManager.Presentation.ConsoleApp;

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
        services.AddSingleton(config);
        CloudFileManager.Infrastructure.DependencyRegister.Register(services, config, basePath);
        CloudFileManager.Application.DependencyRegister.Register(services, config, basePath);
        services.AddSingleton<IConsoleCommandParser, ConsoleCommandParser>();
        services.AddSingleton<ConsoleSessionState>();
        services.AddScoped<IConsoleCommandExecutor, ConsoleCommandExecutor>();
        services.AddSingleton<ConsoleCommandLoop>();
    }

    /// <summary>
    /// 處理初始化作業。
    /// </summary>
    public static void Initialize(IServiceProvider serviceProvider, bool shouldMigrate = true)
    {
        CloudFileManager.Infrastructure.DependencyRegister.Initialize(serviceProvider, shouldMigrate);
    }

    /// <summary>
    /// 以非同步方式處理初始化作業。
    /// </summary>
    public static Task InitializeAsync(IServiceProvider serviceProvider, bool shouldMigrate = true, CancellationToken cancellationToken = default)
    {
        return CloudFileManager.Infrastructure.DependencyRegister.InitializeAsync(serviceProvider, shouldMigrate, cancellationToken);
    }
}
