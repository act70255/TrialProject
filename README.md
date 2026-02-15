# CloudFileManager

以 C# / .NET 實作的雲端檔案管理示範專案，提供三個入口：`Console`、`Web API`、`Website`。

目標是用 **MVP + 分層架構** 展示的檔案管理系統設計。

## 功能重點

- 目錄與檔案操作：建立、上傳、搬移、重新命名、刪除、下載。
- 查詢與輸出：目錄樹、容量計算、副檔名搜尋、XML 匯出、Feature Flags。
- 分層與解耦：Domain / Application / Infrastructure / Presentation 明確分責。
- 多入口一致語意：Console、Web API、Website 對齊同一套核心用例。

## 專案結構（精簡）

### src

| 專案 | 角色 | 主要依賴 |
| --- | --- | --- |
| `CloudFileManager.Domain` | 領域模型與規則 | 無 |
| `CloudFileManager.Contracts` | DTO 契約 | 無 |
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

## 快速開始

本專案提供兩種主要的操作模式：圖形化的網頁介面與指令行的主控台介面。請根據您的需求選擇一種方式啟動。

### 模式一：使用 Docker Compose (建議，適用 Web 操作)

此方式會一次啟動所有服務（Website, WebApi, DB），讓您可以直接透過瀏覽器操作檔案系統。這是體驗完整功能最快的方式。

**啟動步驟：**

1.  請先確認您已安裝 Docker 與 Docker Compose。
2.  在專案根目錄下，執行以下指令：
    ```bash
    docker compose -f docker-compose.sample.yml up -d
    ```
3.  等待容器啟動後，開啟瀏覽器連至 `http://localhost:5290` 即可開始操作。

### 模式二：使用 dotnet run (建議，適用 Console 操作)

如果您需要執行批次或指令行的操作（例如：上傳本地檔案），建議直接在本機環境執行主控台應用程式。這樣可以最直觀地存取您電腦中的檔案。

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
4.  根據畫面提示即可進行操作（可輸入 `help` 查看所有指令）。

## 最小使用索引

- Console 指令：`help`、`tree`、`ls`、`cd`、`pwd`、`size`、`search`、`mkdir`、`upload`、`move-file`、`rename-file`、`delete-file`、`download`、`move-dir`、`rename-dir`、`delete-dir`、`xml`、`flags`。
- Web API 主要端點：
  - `GET /api/filesystem/tree`
  - `POST /api/filesystem/directories`
  - `POST /api/filesystem/files/upload-form`
  - `GET /api/filesystem/files/content`
  - `GET /api/filesystem/size`
  - `GET /api/filesystem/search`
  - `GET /api/filesystem/xml`
  - `GET /api/filesystem/feature-flags`

## 設定重點

主要設定檔：

- `src/CloudFileManager.Presentation/CloudFileManager.Presentation.Console/appsettings.json`
- `src/CloudFileManager.Presentation/CloudFileManager.Presentation.WebApi/appsettings.json`
- `src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/appsettings.json`

常用設定鍵：

- `ConfigVersion`
- `Storage.StorageRootPath`
- `Database.Provider` / `Database.ConnectionStrings`
- `Database.MigrateOnStartup`
- `Output.XmlTarget` / `Output.XmlOutputPath`
- `AllowedExtensions`
- `Management.FileConflictPolicy` / `Management.DirectoryDeletePolicy` / `Management.MaxUploadSizeBytes`
- `ApiSecurity.HeaderName` / `ApiSecurity.ApiKey`

> 建議以環境變數覆蓋金鑰（例：`ApiSecurity__ApiKey`），避免正式環境使用預設值。

## 測試與 CI

```powershell
dotnet test TrialProject.sln
```

AC-023 最小驗證：

```powershell
dotnet test tests/CloudFileManager.IntegrationTests/CloudFileManager.IntegrationTests.csproj --filter "Should_ExecuteCoreOperations_WithConsoleEquivalentSemantics"
```

CI Workflow：`.github/workflows/dotnet-ci.yml`（Restore / Build / Test）。

## Docker Compose（環境變數覆蓋）

- 可用 `docker compose` 覆蓋 `appsettings.json`。
- .NET 巢狀鍵名請使用 `__`（例：`Database__Provider=Sqlite`）。
- 參考檔案：`docker-compose.yml`、`docker-compose.sample.yml`。

## 文件索引

- `docs/requirement.md`
- `docs/spec.md`
- `docs/mvp_ac.md`
- `docs/architecture.md`
- `docs/domain-model.md`
- `docs/er-model.md`
- `docs/error-codes.md`
- `docs/config-boundary-checklist.md`
- `docs/task.md`
- `docs/task-dev.md`
- `docs/task-release.md`
- `docs/task-deploy.md`
