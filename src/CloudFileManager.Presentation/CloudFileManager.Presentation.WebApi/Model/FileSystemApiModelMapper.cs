using CloudFileManager.Application.Models;

namespace CloudFileManager.Presentation.WebApi.Model;

public static class FileSystemApiModelMapper
{
    public static CreateDirectoryRequest ToApplication(this CreateDirectoryApiRequest request) =>
        new(request.ParentPath, request.DirectoryName);

    public static UploadFileRequest ToApplication(this UploadFileApiRequest request) =>
        new(request.DirectoryPath, request.FileName, request.Size, request.PageCount, request.Width, request.Height, request.Encoding);

    public static MoveFileRequest ToApplication(this MoveFileApiRequest request) =>
        new(request.SourceFilePath, request.TargetDirectoryPath);

    public static RenameFileRequest ToApplication(this RenameFileApiRequest request) =>
        new(request.FilePath, request.NewFileName);

    public static DeleteFileRequest ToApplication(this DeleteFileApiRequest request) =>
        new(request.FilePath);

    public static DeleteDirectoryRequest ToApplication(this DeleteDirectoryApiRequest request) =>
        new(request.DirectoryPath);

    public static MoveDirectoryRequest ToApplication(this MoveDirectoryApiRequest request) =>
        new(request.SourceDirectoryPath, request.TargetParentDirectoryPath);

    public static RenameDirectoryRequest ToApplication(this RenameDirectoryApiRequest request) =>
        new(request.DirectoryPath, request.NewDirectoryName);

    public static CalculateSizeRequest ToApplication(this CalculateSizeApiRequest request) =>
        new(request.Path);

    public static SearchByExtensionRequest ToApplication(this SearchByExtensionApiRequest request) =>
        new(request.Extension);

    public static DirectoryTreeApiResponse ToApi(this DirectoryTreeResult result) =>
        new(result.Lines);

    public static OperationApiResponse ToApi(this OperationResult result) =>
        new(result.Success, result.Message, result.ErrorCode);

    public static SizeCalculationApiResponse ToApi(this SizeCalculationResult result) =>
        new(result.IsFound, result.SizeBytes, result.FormattedSize, result.TraverseLog);

    public static SearchApiResponse ToApi(this SearchResult result) =>
        new(result.Paths, result.TraverseLog);

    public static XmlExportApiResponse ToApi(this XmlExportResult result) =>
        new(result.XmlContent, result.OutputPath);

    public static FeatureFlagsApiResponse ToApi(this FeatureFlagsResult result) =>
        new(result.Flags);
}
