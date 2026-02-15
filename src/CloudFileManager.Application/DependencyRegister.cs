using CloudFileManager.Application.Configuration;
using CloudFileManager.Application.Implementations;
using CloudFileManager.Application.Interfaces;
using CloudFileManager.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CloudFileManager.Application;

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
        services.AddSingleton<CloudFileFactoryRegistry>(_ => new CloudFileFactoryRegistry([
            new WordFileFactory(),
            new ImageFileFactory(),
            new TextFileFactory()
        ]));
        services.AddSingleton<IFileMetadataExtractor, WordFileMetadataExtractor>();
        services.AddSingleton<IFileMetadataExtractor, ImageFileMetadataExtractor>();
        services.AddSingleton<IFileMetadataExtractor, TextFileMetadataExtractor>();
        services.AddSingleton<ILocalFileUploadRequestFactory, LocalFileUploadRequestFactory>();
        services.AddScoped<CloudDirectory>(serviceProvider =>
        {
            IStorageMetadataGateway gateway = serviceProvider.GetRequiredService<IStorageMetadataGateway>();
            return gateway.LoadRootTree();
        });
        services.AddScoped<ICloudFileFileCommandService, CloudFileFileCommandService>();
        services.AddScoped<ICloudFileDirectoryCommandService, CloudFileDirectoryCommandService>();
        services.AddScoped<ICloudFileReadModelService>(serviceProvider =>
        {
            CloudDirectory root = serviceProvider.GetRequiredService<CloudDirectory>();
            IXmlOutputWriter xmlOutputWriter = serviceProvider.GetRequiredService<IXmlOutputWriter>();
            return new CloudFileReadModelService(root, config, basePath, xmlOutputWriter);
        });
        services.AddScoped<ICloudFileApplicationService>(serviceProvider =>
        {
            ICloudFileReadModelService readModelService = serviceProvider.GetRequiredService<ICloudFileReadModelService>();
            ICloudFileFileCommandService fileCommandService = serviceProvider.GetRequiredService<ICloudFileFileCommandService>();
            ICloudFileDirectoryCommandService directoryCommandService = serviceProvider.GetRequiredService<ICloudFileDirectoryCommandService>();
            return new CloudFileApplicationService(readModelService, fileCommandService, directoryCommandService);
        });
    }
}
