# Docker 快速指南

僅保留 `UP`、`DOWN` 與 Sample 模式的測試方式。

## SAMPLE Quick Start

```bash
git clone <repo-url>
docker compose -f docker-compose.sample.yml up -d
docker compose -f docker-compose.sample.yml exec console sh
# 進入容器環境後，可用指令進入 Console 介面
dotnet CloudFileManager.Presentation.Console.dll
```

## UP

正式模式：

```bash
docker compose -f docker-compose.yml up -d --build
```

Sample 模式：

```bash
docker compose -f docker-compose.sample.yml up -d --build
```

## DOWN

正式模式：

```bash
docker compose -f docker-compose.yml down
```

Sample 模式：

```bash
docker compose -f docker-compose.sample.yml down
```

## SAMPLE 測試方式

### Console 測試

單次執行（跑完即移除）：

```bash
docker compose -f docker-compose.sample.yml run --rm console
```

### Web 測試

可透過系統環境變數設定以下 Sample 對外埠：

- `SAMPLE_WEBSITE_PORT`
- `SAMPLE_WEBAPI_PORT`

`docker-compose.sample.yml` 的預設值為：

- `SAMPLE_WEBSITE_PORT=5290`
- `SAMPLE_WEBAPI_PORT=5281`

```text
# 啟動後於瀏覽器開啟：
http://localhost:${SAMPLE_WEBSITE_PORT}
# 若要測試 API：
http://localhost:${SAMPLE_WEBAPI_PORT}
```

若使用預設值，網址為：
```text
Website: http://localhost:5290
WebApi:  http://localhost:5281
```
