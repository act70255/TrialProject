# Web 操作指南

此文件整理 `Web API`、`Website` 與 `Docker Compose` 的實際操作方式。

## 部署原則

- 主要服務為 `webapi` 與 `website`。
- `console` 僅作為除錯/維運工具，建議使用 profile 按需啟動，不作為常駐服務。
- Database migration 由 `webapi` 執行；`console` 會設定 `Database__MigrateOnStartup=false` 以避免 SQLite 鎖定衝突。

## Terminal（dotnet run）

### 啟動服務指令

> 需開啟兩個終端機，分別啟動 `WebApi` 與 `Website`。

| 服務 | 指令 |
|------|------|
| WebApi | `dotnet run --project src/CloudFileManager.Presentation/CloudFileManager.Presentation.WebApi/CloudFileManager.Presentation.WebApi.csproj` |
| Website | `dotnet run --project src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/CloudFileManager.Presentation.Website.csproj` |

### 服務網址

> 以下為 `launchSettings.json` 的 Development 預設值。

| 項目 | URL | 備註 |
|------|-----|------|
| Website | `http://localhost:5189` | 前端網站入口 |
| Swagger UI | `http://localhost:5181/swagger` | `WebApi` 文件頁 |
| OpenAPI JSON | `http://localhost:5181/openapi/v1.json` | 規格檔 |

## Docker（docker compose）

### 啟動服務指令

| 模式 | 指令 | 說明 |
|------|------|------|
| 一般 compose | `docker compose -f docker-compose.yml up -d webapi website` | 明確指定 `docker-compose.yml` |
| sample compose | `docker compose -f docker-compose.sample.yml up -d webapi website` | 使用 `docker-compose.sample.yml` |
| 一般 compose 日誌 | `docker compose -f docker-compose.yml logs -f webapi website` | 即時查看兩服務日誌 |
| sample compose 日誌 | `docker compose -f docker-compose.sample.yml logs -f webapi website` | 即時查看兩服務日誌 |

### 停止與清理

| 模式 | 動作 | 指令 | 備註 |
|------|------|------|------|
| 一般 compose | 停止服務 | `docker compose -f docker-compose.yml stop webapi website` | 保留容器 |
| 一般 compose | 停止並移除 | `docker compose -f docker-compose.yml down` | 移除容器與 network |
| 一般 compose | 完整清理 | `docker compose -f docker-compose.yml down -v` | 會移除 volume（資料會刪除） |
| sample compose | 停止服務 | `docker compose -f docker-compose.sample.yml stop webapi website` | 保留容器 |
| sample compose | 停止並移除 | `docker compose -f docker-compose.sample.yml down` | 移除容器與 network |
| sample compose | 完整清理 | `docker compose -f docker-compose.sample.yml down -v` | 會移除 volume（資料會刪除） |

### 服務網址

| 模式 | Website URL | Swagger UI | OpenAPI JSON |
|------|-------------|------------|--------------|
| 一般 compose (`docker-compose.yml`) | `http://localhost:5190` | 不提供（`UseSwagger=false`） | 不提供 |
| sample compose (`docker-compose.sample.yml`) | `http://localhost:5290` | `http://localhost:5281/swagger` | `http://localhost:5281/openapi/v1.json` |

## Console（選用）

僅在需要互動式命令列時啟動：

```bash
docker compose -f docker-compose.yml --profile console up console
```

## Web API 主要端點

### 基本操作

- `GET /api/filesystem/tree`
- `POST /api/filesystem/directories`
- `POST /api/filesystem/files/upload-form`
- `GET /api/filesystem/files/content`
- `GET /api/filesystem/size`
- `GET /api/filesystem/search`
- `GET /api/filesystem/xml`
- `GET /api/filesystem/feature-flags`

### State 驅動操作（Website 主要使用）

- `POST /api/filesystem/directories/entries/query`
- `POST /api/filesystem/directories/change-current`
- `POST /api/filesystem/directories/sort`
- `POST /api/filesystem/clipboard/copy`
- `POST /api/filesystem/clipboard/paste`
- `POST /api/filesystem/tags/assign`
- `POST /api/filesystem/tags/remove`
- `POST /api/filesystem/tags/list`
- `POST /api/filesystem/tags/find`
- `POST /api/filesystem/history/undo`
- `POST /api/filesystem/history/redo`
- `POST /api/filesystem/search/query`
- `POST /api/filesystem/xml/export`

## Web 相關設定檔

- `src/CloudFileManager.Presentation/CloudFileManager.Presentation.WebApi/appsettings.json`
- `src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/appsettings.json`
- `src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/appsettings.Development.json`
- `docker-compose.yml`
- `docker-compose.sample.yml`

## Docker Compose 設定補充

- 可用 `docker compose` 覆蓋 `appsettings.json`。
- .NET 巢狀鍵名請使用 `__`（例：`Database__Provider=Sqlite`）。
- 主要環境變數建議：
  - `ApiSecurity__ApiKey`
  - `Database__ConnectionStrings__Sqlite`
  - `Storage__StorageRootPath`
  - `Output__XmlOutputPath`

## API 安全設定

- `ApiSecurity.HeaderName`
- `ApiSecurity.ApiKey`

正式環境需要以環境變數覆蓋金鑰 避免正式環境使用預設值。
