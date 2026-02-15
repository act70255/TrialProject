# 設定分層檢查清單（WebApi / Website）

## 1. 原則
- `WebApi`（後端）負責系統與業務設定：資料庫、儲存、規則、功能旗標。
- `Website`（前端站台）只保留 UI 站台設定：API Base URL、站台自身 Logging。
- 兩邊可出現同名鍵（例如 `Logging`、`AllowedHosts`），因為是獨立程序；只要語意不重複即可。

## 2. WebApi 必須承載的設定
- `ConfigVersion`
- `Storage.*`
- `Database.*`
- `Traversal.*`
- `Output.*`
- `AllowedExtensions.*`
- `FeatureFlags.*`
- `Management.*`

## 3. Website 建議保留的設定
- `WebApiBaseUrl`
- 站台自身 `Logging.*`
- 站台自身 `AllowedHosts`

## 4. 不應放在 Website 的設定
- 任一資料庫連線字串（`Database.ConnectionStrings.*`）
- 任一檔案儲存根目錄（`Storage.StorageRootPath`）
- 任一業務規則或功能旗標（`Management.*`、`FeatureFlags.*`）
- 任一檔案型別與輸出策略（`AllowedExtensions.*`、`Output.*`）

## 5. 快速檢查步驟
- 打開 `src/CloudFileManager.Presentation/CloudFileManager.Presentation.WebApi/appsettings.json`，確認系統設定都集中在此。
- 打開 `src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/appsettings.json`，確認只有站台設定與 API 位址。
- 若新增設定鍵，先判斷「是否屬於業務或基礎設施」：是則放 `WebApi`，否則才考慮放 `Website`。

## 6. 常見衝突與處理
- Website 打到錯誤 API：檢查 `WebApiBaseUrl` 與 WebApi 啟動 Port 是否一致。
- 本機可跑、部署失敗：確認部署環境是否覆蓋了 `WebApiBaseUrl` 或 WebApi 端資料庫設定。
- 設定散落兩邊：以 `WebApi` 為單一真實來源（SSOT），刪除 Website 的重複業務鍵。
