# Web 操作指南

此文件整理原 README 中與 `Web API`、`Website`、`Docker Compose` 相關內容。

## 啟動方式（Docker Compose）

1. 確認已安裝 Docker 與 Docker Compose。
2. 在專案根目錄執行：

```bash
docker compose -f docker-compose.sample.yml up -d
```

3. 等待容器啟動後，開啟 `http://localhost:5290`。

## Web API 主要端點

- `GET /api/filesystem/tree`
- `POST /api/filesystem/directories`
- `POST /api/filesystem/files/upload-form`
- `GET /api/filesystem/files/content`
- `GET /api/filesystem/size`
- `GET /api/filesystem/search`
- `GET /api/filesystem/xml`
- `GET /api/filesystem/feature-flags`

## Web 相關設定檔

- `src/CloudFileManager.Presentation/CloudFileManager.Presentation.WebApi/appsettings.json`
- `src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/appsettings.json`

## Docker Compose 設定補充

- 可用 `docker compose` 覆蓋 `appsettings.json`。
- .NET 巢狀鍵名請使用 `__`（例：`Database__Provider=Sqlite`）。
- 參考檔案：`docker-compose.yml`、`docker-compose.sample.yml`。

## API 安全設定

- `ApiSecurity.HeaderName`
- `ApiSecurity.ApiKey`

建議以環境變數覆蓋金鑰（例：`ApiSecurity__ApiKey`），避免正式環境使用預設值。
