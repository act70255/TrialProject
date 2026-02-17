# CloudFileManager

以 C# / .NET 實作的雲端檔案管理示範專案，主軸為 `Console` 互動驗證。

目標是用 **分層架構** 展示的檔案管理系統設計。

## 功能重點

- 目錄與檔案操作：建立、上傳、搬移、重新命名、刪除、下載。
- 查詢與輸出：目錄樹、容量計算、副檔名搜尋、XML 匯出、Feature Flags。
- 分層與解耦：Domain / Application / Infrastructure / Presentation 明確分責。
- 核心驗證以 Console 為主，Web 相關說明獨立整理於 `docs/web.md`。

## 專案結構（精簡）

### src

| 專案 | 角色 | 主要依賴 |
| --- | --- | --- |
| `CloudFileManager.Domain` | 領域模型與規則 | 無 |
| `CloudFileManager.Contracts` | DTO | 無 |
| `CloudFileManager.Shared` | 共用型別與錯誤碼 | 無 |
| `CloudFileManager.Application` | 用例協調、介面定義與實作 | Domain / Shared |
| `CloudFileManager.Infrastructure` | EF Core、檔案儲存、設定載入 | Application / Domain / Shared |
| `CloudFileManager.Presentation.Console` | CLI 互動入口 | Application / Infrastructure / Contracts / Shared |
| `CloudFileManager.Presentation.WebApi` | HTTP API 入口 | Application / Infrastructure / Contracts / Shared |
| `CloudFileManager.Presentation.Website` | MVC Website（透過 HTTP Client 呼叫 WebApi） | Contracts / Shared |

### tests

| 專案 | 目的 |
| --- | --- |
| `CloudFileManager.UnitTests` | 驗證 Domain / Application 規則 |
| `CloudFileManager.IntegrationTests` | 驗證 WebApi + Website + Infrastructure 邊界整合 |

## 快速開始（Console）

若您需要執行批次或指令行操作（例如：上傳本地檔案），建議直接在本機執行 Console 應用程式。

**啟動步驟：**

1.  請先確認您已安裝 .NET SDK (版本需求請見 `global.json`)。
2.  執行專案還原與建置：
    ```powershell
    dotnet restore TrialProject.sln
    dotnet build TrialProject.sln
    ```
3.  啟動主控台互動介面：
    ```powershell
    dotnet run --project src/CloudFileManager.Presentation/CloudFileManager.Presentation.Console/CloudFileManager.Presentation.Console.csproj
    ```
4.  根據畫面提示即可進行操作（可輸入 `?` 查看所有指令）。

> Web API / Website 與 Docker Compose 相關操作請見 `docs/web.md`。

## 最小使用索引

- Console 指令：

| 指令 | 說明 |
| --- | --- |
| `help` / `?` | 顯示指令說明 |
| `tree` | 顯示完整目錄樹 |
| `ls` | 顯示當前目錄內容 |
| `cd <path>` | 切換目前目錄 |
| `size <path>` | 計算目錄總容量（含遞迴） |
| `search <ext> <path>` | 依副檔名遞迴搜尋 |
| `mkdir <name>` | 建立目錄 |
| `upload <path> <localFile>` | 上傳本機檔案 |
| `move-file <src> <targetDir>` | 搬移檔案 |
| `rename-file <path> <newName>` | 重新命名檔案 |
| `delete-file <path>` | 刪除檔案 |
| `download <path> <localPath>` | 下載檔案到本機 |
| `move-dir <src> <targetParent>` | 搬移目錄 |
| `rename-dir <path> <newName>` | 重新命名目錄 |
| `delete-dir <path>` | 刪除目錄 |
| `xml [path] [raw]` | 輸出 XML（語意版或 raw） |
| `flags` | 顯示功能旗標 |
- Web API / Website 操作與端點清單請見 `docs/web.md`。

## 設定重點

主要設定檔：

- `src/CloudFileManager.Presentation/CloudFileManager.Presentation.Console/appsettings.json`

> Web API / Website 設定檔請見 `docs/web.md`。

常用設定鍵：

| 設定鍵 | 說明 |
| --- | --- |
| `ConfigVersion` | 設定檔版本識別，用於版本對齊與檢核。 |
| `Storage.StorageRootPath` | 儲存根目錄位置（檔案與資料目錄基準路徑）。 |
| `Database.Provider` | 資料庫提供者（例如 SQLite / SQL Server）。 |
| `Database.ConnectionStrings` | 各資料庫連線字串設定。 |
| `Database.MigrateOnStartup` | 啟動時是否自動執行 migration。 |
| `Output.XmlTarget` | XML 輸出目標模式。 |
| `Output.XmlOutputPath` | XML 輸出檔案路徑。 |
| `AllowedExtensions` | 允許上傳/處理的副檔名白名單。 |
| `Management.FileConflictPolicy` | 檔案衝突處理策略（例如 Reject/Overwrite）。 |
| `Management.DirectoryDeletePolicy` | 目錄刪除策略（例如禁止刪除非空目錄）。 |
| `Management.MaxUploadSizeBytes` | 上傳容量上限（Bytes）。 |

> API 金鑰與 Web 相關設定請見 `docs/web.md`。

## 驗證流程入口

- 請依 [驗收清單（Acceptance Criteria）](./delivery/AcceptanceCriteria.md) 的章節順序執行驗證（C0 -> C4.4 -> C5 -> D1~D5）。
- 驗證證據與勾選狀態統一維護於 [驗收清單（Acceptance Criteria）](./delivery/AcceptanceCriteria.md)。

## 模型對照

- 領域模型（UML）：[UML 類別圖（UML Class Diagram）](./delivery/UMLClassDirgram.md)
- 資料模型（ER）：[ER 模型（ER Model）](./delivery/ERModel.md)

## 測試與 CI

```powershell
dotnet test TrialProject.sln
```

AC-023 最小驗證：

```powershell
dotnet test tests/CloudFileManager.IntegrationTests/CloudFileManager.IntegrationTests.csproj --filter "Should_ExecuteCoreOperations_WithConsoleEquivalentSemantics"
```

CI Workflow：`.github/workflows/dotnet-ci.yml`（Restore / Build / Test）。

## 文件索引

- [驗收清單（Acceptance Criteria）](./delivery/AcceptanceCriteria.md)
- [UML 類別圖（UML Class Diagram）](./delivery/UMLClassDirgram.md)
- [ER 模型（ER Model）](./delivery/ERModel.md)
