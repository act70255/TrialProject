using CloudFileManager.Application.Models;

namespace CloudFileManager.Application.Interfaces;

public interface ICloudFileReadModelService
{
    DirectoryTreeResult GetDirectoryTree();

    SizeCalculationResult CalculateTotalSize(CalculateSizeRequest request);

    SearchResult SearchByExtension(SearchByExtensionRequest request);

    DirectoryEntriesResult GetDirectoryEntries(ListDirectoryEntriesRequest request);

    XmlExportResult ExportXml(ExportXmlRequest? request = null);

    FeatureFlagsResult GetFeatureFlags();
}
