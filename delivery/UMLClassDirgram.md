# 交付文件 UML 系統架構

以下圖面採「跨層職責區分」方式呈現，將 Presentation / Application / Infrastructure / Data 明確分層（使用 flowchart）。

```mermaid
flowchart TD
    subgraph P[表現層 Presentation]
      A[Console Input<br/>size/search/xml]
      B[ConsoleCommandExecutor]
      L[Console Result + Traverse Log]
    end

    subgraph APL[應用層 Application]
      C[ICloudFileApplicationService]
      D{Query or Command}
      E[CloudFileReadModelService]
      F[File/Directory Command Service]
      G[CloudDirectory Root Tree]
      H[IXmlOutputWriter]
    end

    subgraph INF[基礎設施層 Infrastructure]
      subgraph INF_DB[資料存取 DB Adapter]
        I[IStorageMetadataGateway / StorageMetadataGateway]
      end
      subgraph INF_FILE[檔案輸出 File IO Adapter]
        M[IXmlOutputWriter / FileSystemXmlOutputWriter]
      end
    end

    subgraph DATA[資料層 Data]
      subgraph DATA_DB[資料庫儲存 DB Store]
        J[(SQLite/SQL Storage)]
      end
      subgraph DATA_FILE[檔案儲存 File Store]
        K[(XML Output File)]
      end
    end

    A --> B
    B --> C
    C --> D
    D -->|查詢 Query| E
    D -->|異動 Command| F
    E --> G
    E --> H
    F --> I
    E --> M
    I --> J
    M --> K
    E --> L
```
---
# 交付文件 - UML Class Diagram

此圖以目前程式碼實作為基準，來源主要對照：

- `src/CloudFileManager.Domain/FileSystemNode.cs`
- `src/CloudFileManager.Domain/CloudDirectory.cs`
- `src/CloudFileManager.Domain/CloudFile.cs`
- `src/CloudFileManager.Domain/WordFile.cs`
- `src/CloudFileManager.Domain/ImageFile.cs`
- `src/CloudFileManager.Domain/TextFile.cs`
- `src/CloudFileManager.Domain/Enums/CloudFileType.cs`

```mermaid
classDiagram
    direction TB

    class FileSystemNode {
      <<abstract>>
      - string Name
      - DateTime CreatedTime
      + Rename(newName: string) void
    }

    class CloudDirectory {
      - List~CloudDirectory~ _directories
      - List~CloudFile~ _files
      + Directories: IReadOnlyList~CloudDirectory~
      + Files: IReadOnlyList~CloudFile~
      + AddDirectory(name: string, createdTime: DateTime) CloudDirectory
      + AddFile(file: CloudFile) void
      + RemoveFile(fileName: string) bool
      + RemoveDirectory(directoryName: string) bool
      + DetachDirectory(directoryName: string) CloudDirectory?
      + AttachDirectory(directory: CloudDirectory) void
      + CalculateTotalBytes(traverseLog: List~string~?, currentPath: string) long
    }

    class CloudFile {
      <<abstract>>
      - long Size
      - CloudFileType FileType
      + DetailText: string
    }

    class WordFile {
      - int PageCount
      + DetailText: string
    }

    class ImageFile {
      - int Width
      - int Height
      + DetailText: string
    }

    class TextFile {
      - string Encoding
      + DetailText: string
    }

    class CloudFileType {
      <<enumeration>>
      Word
      Image
      Text
    }

    FileSystemNode <|-- CloudDirectory : Inheritance
    FileSystemNode <|-- CloudFile : Inheritance
    CloudFile <|-- WordFile : Inheritance
    CloudFile <|-- ImageFile : Inheritance
    CloudFile <|-- TextFile : Inheritance

    CloudDirectory *-- "0..*" CloudDirectory : Composition (subdirectories)
    CloudDirectory *-- "0..*" CloudFile : Composition (files)
    CloudFile --> CloudFileType : Association (type)
```

## 指定關係對照

- Inheritance
  - `FileSystemNode <- CloudDirectory`
  - `FileSystemNode <- CloudFile`
  - `CloudFile <- WordFile/ImageFile/TextFile`
- Association
  - `CloudFile -> CloudFileType`（檔案型別關聯）
- Composition
  - `CloudDirectory *-- CloudDirectory`（子目錄）
  - `CloudDirectory *-- CloudFile`（檔案）

## 驗收重點（對應 D1）

- 圖上可辨識類別、屬性、操作與導向。
- 圖上明確包含 Inheritance、Association、Composition。
- 與實作一致：目錄樹遞迴結構與三種檔案型別皆有對應。

---
