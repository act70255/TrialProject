# 交付文件 - UML Class Diagram

此文件為交付版 UML Class Diagram，已依目前程式碼更新（`src/CloudFileManager.Domain` 與 `src/CloudFileManager.Application/Interfaces`）。

```mermaid
classDiagram
    class FileSystemNode {
      <<abstract>>
      +string Name
      +DateTime CreatedTime
      +Rename(string newName)
    }

    class CloudFile {
      <<abstract>>
      +long Size
      +CloudFileType FileType
      +DateTime CreatedTime
      +string DetailText
    }

    class WordFile {
      +int PageCount
    }

    class ImageFile {
      +int Width
      +int Height
    }

    class TextFile {
      +string Encoding
    }

    class CloudDirectory {
      +IReadOnlyList~CloudDirectory~ Directories
      +IReadOnlyList~CloudFile~ Files
      +AddDirectory(string, DateTime) CloudDirectory
      +AddFile(CloudFile)
      +RemoveFile(string) bool
      +RemoveDirectory(string) bool
      +DetachDirectory(string) CloudDirectory?
      +AttachDirectory(CloudDirectory)
      +CalculateTotalBytes(List~string~, string) long
    }

    class ICloudFileApplicationService {
      <<interface>>
      +GetDirectoryTree() DirectoryTreeResult
      +CreateDirectory(CreateDirectoryRequest) OperationResult
      +UploadFile(UploadFileRequest) OperationResult
      +MoveFile(MoveFileRequest) OperationResult
      +RenameFile(RenameFileRequest) OperationResult
      +DeleteFile(DeleteFileRequest) OperationResult
      +DownloadFile(DownloadFileRequest) OperationResult
      +DownloadFileContent(string) FileDownloadResult
      +DeleteDirectory(DeleteDirectoryRequest) OperationResult
      +MoveDirectory(MoveDirectoryRequest) OperationResult
      +RenameDirectory(RenameDirectoryRequest) OperationResult
      +CalculateTotalSize(CalculateSizeRequest) SizeCalculationResult
      +SearchByExtension(SearchByExtensionRequest) SearchResult
      +ExportXml() XmlExportResult
      +GetFeatureFlags() FeatureFlagsResult
    }

    class CloudFileType {
      <<enumeration>>
      Word
      Image
      Text
    }

    FileSystemNode <|-- CloudFile
    FileSystemNode <|-- CloudDirectory
    CloudFile <|-- WordFile
    CloudFile <|-- ImageFile
    CloudFile <|-- TextFile
    CloudFile --> CloudFileType : uses

    CloudDirectory o-- CloudDirectory : contains
    CloudDirectory o-- CloudFile : contains
    ICloudFileApplicationService --> CloudDirectory : orchestrates
    ICloudFileApplicationService --> CloudFile : orchestrates
```

## 指定關係對照

- Inheritance：`FileSystemNode <|-- CloudFile`、`CloudFile <|-- WordFile` 等
- Association：`ICloudFileApplicationService --> CloudDirectory`、`ICloudFileApplicationService --> CloudFile`
- Aggregation：`CloudDirectory o-- CloudDirectory`、`CloudDirectory o-- CloudFile`
- Enum Association：`CloudFile --> CloudFileType`
