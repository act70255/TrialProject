using CloudFileManager.Application.Models;

namespace CloudFileManager.Application.Interfaces;

public interface ICloudFileReadModelService
{
    DirectoryTreeResult GetDirectoryTree();

    SizeCalculationResult CalculateTotalSize(CalculateSizeRequest request);

    SearchResult SearchByExtension(SearchByExtensionRequest request);

    XmlExportResult ExportXml();

    FeatureFlagsResult GetFeatureFlags();
}
