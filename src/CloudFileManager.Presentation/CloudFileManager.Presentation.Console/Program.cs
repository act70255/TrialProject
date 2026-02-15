using CloudFileManager.Presentation.ConsoleApp;
using CloudFileManager.Infrastructure.Configuration;
using CloudFileManager.Infrastructure.FileStorage;
using Microsoft.Extensions.DependencyInjection;

string configFilePath = ConfigPathResolver.ResolveConfigFilePath(Directory.GetCurrentDirectory(), AppContext.BaseDirectory);
string basePath = ConfigPathResolver.ResolveRuntimeBasePath(configFilePath, Directory.GetCurrentDirectory(), AppContext.BaseDirectory, "TrialProject.sln");
var config = AppConfigLoader.Load(configFilePath);
string storageRootPath = StorageBootstrapper.EnsureStorageRoot(config, basePath);
System.Console.WriteLine($"Storage Root: {storageRootPath}");

ServiceCollection services = new();
DependencyRegister.Register(services, config, basePath);

using ServiceProvider serviceProvider = services.BuildServiceProvider();
await DependencyRegister.InitializeAsync(serviceProvider, shouldMigrate: config.Database.MigrateOnStartup);
ConsoleCommandLoop commandLoop = serviceProvider.GetRequiredService<ConsoleCommandLoop>();
commandLoop.Run();
