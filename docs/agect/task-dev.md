# 雲端檔案管理系統開發任務清單（Development Only）

> 使用方式：完成一項即勾選。
>
> 本清單僅包含「程式開發」相關項目；`【SP-*】` 與 `AC-*` 為追蹤編號。

## A. 開發前技術凍結
- [x] A1. 確認本次開發目標為 MVP（F1~F5）
- [x] A2. 確認遍歷/搜尋順序：DFS Pre-order + 同層建立順序
- [x] A3. 確認 XML 規則：結構與語意一致，不要求逐字節點命名
- [x] A4. 確認容量換算規則：`1KB=1024B`、`1MB=1024KB`
- [x] A5. 確認設定檔驅動原則：路徑與規則不得硬編碼
- [x] A6. 確認 ER Schema 基線採三表（`directories` / `files` / `file_metadata`）並對齊 `docs/er-model.md`【SP-DEL-002】【SP-DOM-010】

## B. 程式骨架
- [x] B1. 建立 C# 專案與可執行程式【SP-DEL-003】
- [x] B2. 建立雙表現層：`Console` 與 `WebApi + Website`【SP-NFR-004】
- [x] B3. 確保 `Website` 僅透過 `WebApi` 操作，不直連 `DataAccess`（`Infrastructure`）【SP-NFR-005】

## C. 領域模型實作
- [x] C1. 建立檔案基底型別（`Name`、`Size`、`CreatedTime`）【SP-DOM-002】
- [x] C2. 建立 Word 檔型別（`PageCount`）【SP-DOM-003】
- [x] C3. 建立 Image 檔型別（`Width`、`Height`）【SP-DOM-004】
- [x] C4. 建立 Text 檔型別（`Encoding`）【SP-DOM-005】
- [x] C5. 建立 Directory 型別（可含檔案與子目錄）【SP-DOM-006】【SP-DOM-007】
- [x] C6. 實作「檔案必屬於目錄」約束【SP-DOM-009】

## D. 正式資料來源
- [x] D1. 建立 `Root` 節點【SP-DATA-001】
- [x] D2. 建立「驗收資料集」`Project_Docs` 與其檔案資料（題目示例）【SP-DATA-004】
- [x] D3. 建立「驗收資料集」`Personal_Notes/Archive_2025` 與其檔案資料（題目示例）【SP-DATA-004】
- [x] D4. 建立「驗收資料集」`README.txt`（ASCII, 500B；題目示例）【SP-DATA-004】
- [x] D5. 正式執行改由「實體儲存 + DB 中繼資料」載入，禁止任何預先植入資料來源【SP-DATA-002】【SP-DATA-003】
- [x] D6. 驗收文件標記資料集類型與版本（題目示例/自訂資料集）【SP-DATA-005】

## E. MVP 功能開發
- [x] E1. F1 目錄結構輸出（Console/WebApi/Website；含檔案細節：大小KB/建立時間）【SP-FUNC-001】【SP-FUNC-002】
- [x] E2. F2 遞迴計算總容量（含子目錄）【SP-FUNC-003】
- [x] E3. F2 容量單位與換算規則（Bytes/KB/MB, 1024 進位）【SP-FUNC-004】【SP-FUNC-005】
- [x] E4. F3 副檔名搜尋與完整路徑回傳【SP-FUNC-006】【SP-FUNC-007】
- [x] E5. F3 走訪順序（DFS Pre-order + 建立順序）【SP-FUNC-008】
- [x] E6. F3 搜尋結果順序依發現順序回傳【SP-FUNC-009】
- [x] E7. F4 XML 輸出方法【SP-FUNC-010】
- [x] E8. F4 XML 輸出符合語意與層級【SP-FUNC-011】
- [x] E9. F5 Traverse Log（計算大小/搜尋；WebApi/Website 需可檢視）【SP-FUNC-012】【SP-FUNC-029】
- [x] E10. F5 Traverse Log 順序與搜尋一致【SP-FUNC-013】
- [x] E11. 驗收資料集可建立至少 3 層目錄深度【SP-FUNC-024】
- [x] E12. 同層建立順序可重現並可由 DFS/Log 驗證【SP-FUNC-025】
- [x] E13. 搜尋無命中副檔名回傳空集合且不拋未處理錯誤【SP-FUNC-027】
- [x] E14. XML 輸出可被標準 XML Parser 成功解析【SP-FUNC-028】

## F. 設定檔與相容性
- [x] F1. 加入 `ConfigVersion` 並驗證版本欄位【SP-CONF-000】
- [x] F2. 以設定檔提供 `StorageRootPath`，移除硬編碼路徑【SP-CONF-001】
- [x] F3. 支援絕對/相對路徑解析【SP-CONF-002】
- [x] F4. 啟動時自動建立不存在的儲存目錄【SP-CONF-003】
- [x] F5. 遍歷策略設定（預設 DFS_PRE_ORDER + CREATION_ORDER）【SP-CONF-004】
- [x] F6. Logging 等級設定（Info/Debug）【SP-CONF-005】
- [x] F7. XML 輸出目標設定（Console/檔案）【SP-CONF-006】
- [x] F8. 副檔名白名單依類型分組【SP-CONF-007】【SP-CONF-008】
- [x] F9. 副檔名正規化規則（小寫、`.` 開頭、不分大小寫）【SP-CONF-009】
- [x] F10. 建檔時副檔名驗證與錯誤回報【SP-CONF-010】
- [x] F11. 搜尋前輸入副檔名正規化【SP-CONF-011】
- [x] F12. `AllowedExtensions` 缺值時套用預設值【SP-CONF-012】
- [x] F13. 設定驗證錯誤格式（`ErrorCode`/`Field`/`Message`）【SP-CONF-013】
- [x] F14. 功能旗標（Feature Flags）設定與讀取【SP-CONF-014】
- [x] F15. 設定欄位向後相容策略（新增不破壞）【SP-CONF-015】

## G. 架構擴充與維護性
- [x] G1. 新檔案類型走擴充契約（介面/抽象基底）【SP-EXT-001】
- [x] G2. 採 Registry/Factory 註冊式設計【SP-EXT-002】
- [x] G3. 落實三層分離（Presentation/Domain/DataAccess；DataAccess 實作專案為 `Infrastructure`）【SP-EXT-003】
- [x] G4. 設定缺值預設策略與關鍵欄位阻擋啟動【SP-EXT-004】
- [x] G5. 規則變更優先走設定檔，不改核心流程【SP-EXT-005】
- [x] G6. 每個新增設定項補齊合法/非法值測試【SP-EXT-006】
- [x] G7. 解決方案專案基線固定為 `Presentation`、`Domain`、`DataAccess`（實作專案名稱：`Infrastructure`）【SP-NFR-011】
- [x] G8. `Domain` 維持可獨立發佈邊界（可打包 lib/NuGet）【SP-NFR-012】
- [x] G9. `Presentation` 對 `DataAccess`（`Infrastructure`）參考僅限 Composition Root 的 DI 註冊【SP-NFR-013】
- [x] G10. 僅在觸發條件成立時才抽出 `Domain.Abstractions`【SP-NFR-014】
- [x] G11. `Presentation` 對外 I/O 改用通道專屬模型，並在邊界映射至 `Contracts`【SP-NFR-015】
- [x] G12. `Contracts` 契約改為扁平命名空間，移除 `Requests` / `Responses` 分層【SP-NFR-016】
- [x] G13. 專案目錄結構語意一致化（`Infrastructure` 分責 + `WebApi/Model` 集中管理）【SP-NFR-017】

## H. 管理操作（Phase 2 / MMP）
> 本章為可用性強化，不影響 MVP 通關。

- [x] H1. 檔案上傳（Upload）至指定目錄【SP-MGMT-001】
- [x] H2. 檔案下載（Download）至指定路徑【SP-MGMT-002】
- [x] H3. 檔案搬移（Move）與非法路徑防呆【SP-MGMT-003】
- [x] H4. 檔案重新命名（Rename）與同層衝突檢查【SP-MGMT-004】
- [x] H5. 檔案刪除（Delete）與錯誤回報【SP-MGMT-005】
- [x] H6. 建立資料夾（Create Directory）【SP-MGMT-006】
- [x] H7. 刪除資料夾（Delete Directory）【SP-MGMT-007】
- [x] H8. 資料夾刪除策略（ForbidNonEmpty / RecursiveDelete）【SP-MGMT-008】
- [x] H9. 資料夾搬移（Move Directory）與循環防呆【SP-MGMT-009】
- [x] H10. 資料夾重新命名（Rename Directory）【SP-MGMT-010】
- [x] H11. 名稱衝突策略設定（FileConflictPolicy）【SP-MGMT-011】
- [x] H12. 上傳大小限制設定（MaxUploadSizeBytes）【SP-MGMT-012】
- [x] H13. 操作稽核紀錄（時間/路徑/結果）【SP-MGMT-013】

## I. 開發階段驗證
- [x] I1. 驗收 AC-001：目錄樹建立與輸出正確
- [x] I2. 驗收 AC-002：總容量計算與單位規則正確
- [x] I3. 驗收 AC-003：副檔名搜尋結果與順序正確
- [x] I4. 驗收 AC-004：XML 輸出符合語意與層級
- [x] I5. 驗收 AC-005：Traverse Log 可驗證遍歷過程
- [x] I6. 驗收 AC-006：儲存路徑配置跨環境可運作
- [x] I7. 驗收 AC-007：Logging/XML 輸出行為可切換
- [x] I8. 驗收 AC-008：副檔名分組規則可運作
- [x] I9. 驗收 AC-009：設定版本與相容性策略可運作
- [x] I10. 驗收 AC-010：擴充機制與功能旗標可運作
- [x] I11. 驗收 AC-011：分層與維護性要求落實
- [x] I12. 驗收 AC-012：雙表現層分工與依賴邊界正確
- [x] I13. 驗收 AC-019：三專案分層與 DI 組裝邊界符合規範
- [x] I14. 驗收 AC-020：Domain 可獨立打包且契約拆分策略具可控觸發條件
- [x] I15. 驗收 AC-024：初始化資料集深度與建立順序可支撐遞迴/遍歷驗證
- [x] I16. 驗收 AC-025：容量驗收具人工換算與程式輸出比對證據
- [x] I17. 驗收 AC-026：無命中搜尋回空集合且行為穩定
- [x] I18. 驗收 AC-027：XML 輸出結構合法且可被 Parser 解析

## J. SQLite 正式化與 SQL Server 相容性
- [x] J1. `DataAccess`（`Infrastructure`）導入 EF Core 並以 SQLite Provider 為預設【SP-NFR-006】
- [x] J1a. 建立 EF Core Code First 基礎模型與 DbContext【SP-NFR-009】
- [x] J1b. 建立 Initial Migration 並定義 Schema 版本化流程【SP-NFR-009】
- [x] J1c. 落地 `files` 與 `file_metadata` 的 1:1 與複合 FK（`file_metadata(file_id,file_type) -> files(id,file_type)`）【SP-DOM-012】【SP-DOM-013】
- [x] J1d. 落地唯一性與型別合法性約束（Unique + Check 或等價驗證）【SP-DOM-014】【SP-DOM-015】
- [x] J2. 補齊 `Database.Provider` 與雙連線字串設定（Sqlite/SqlServer）【SP-CONF-016】【SP-CONF-017】
- [x] J2a. 設定模型新增 `Database.Provider` 與雙連線字串欄位【SP-CONF-016】【SP-CONF-017】
- [x] J2b. 設定驗證補齊：Provider 合法值、預設值、對應連線字串必填【SP-CONF-018】【SP-CONF-019】【SP-CONF-020】
- [x] J2c. DI 依 `Database.Provider` 分支註冊 `UseSqlite` / `UseSqlServer`【SP-NFR-006】【SP-NFR-008】
- [x] J2d. 補齊啟動失敗與錯誤格式測試（非法 Provider、缺連線字串）【SP-CONF-020】【SP-EXT-006】
- [x] J2e. 加入 `Database.MigrateOnStartup` 設定，預設 `false`【SP-CONF-021】
- [x] J2f. 啟動流程依環境策略執行 Migration（Dev/Test 可自動，Production 預設關閉）【SP-CONF-021】
- [x] J3. Upload 流程完成「寫入 StorageRootPath + 寫入 DB 中繼資料」一致性【SP-MGMT-014】
- [x] J4. Move/Rename/Delete 流程同步更新實體檔案與 DB 中繼資料【SP-MGMT-015】
- [x] J5. 移除 Production 對預先植入資料來源的依賴，僅允許正式資料來源【SP-DATA-003】
- [x] J6. 建立 Provider 相容整合測試（SQLite 必跑、SQL Server 可切換驗證）【SP-NFR-007】
- [x] J7. 驗收 AC-013：實體儲存與中繼資料一致性
- [x] J8. 驗收 AC-014：SQLite 預設執行與 SQL Server 相容切換成立
- [x] J9. 驗收 AC-015：Provider 設定合法性與啟動失敗機制成立
- [x] J10. 驗收 AC-016：Provider 切換不影響 `Domain` 契約
- [x] J11. 驗收 AC-017：Code First 與 Migration 版本化策略成立
- [x] J12. 建立 CI 自動檢查（組態驗證、測試、Migration 狀態檢查）【SP-NFR-010】
- [x] J13. 驗收 AC-018：自動檢查與 Migration 策略成立
- [x] J14. 驗收 AC-021：ER Schema 與資料一致性約束落地

## K. WebApi / Website 設定分層
- [x] K1. 確認系統級設定集中於 `WebApi`（Storage/Database/FeatureFlags/Management）【SP-CONF-022】
- [x] K2. 確認 `Website` 僅保留站台層設定（`WebApiBaseUrl`、站台 Logging/AllowedHosts）【SP-CONF-023】
- [x] K3. 確認 `Website` 無後端業務設定重複鍵（DB/Storage/Output/AllowedExtensions/Management/FeatureFlags）【SP-CONF-024】
- [x] K4. 文件化設定分層檢查清單並納入日常檢查【SP-CONF-026】
- [x] K5. 驗收 AC-022：設定分層責任清楚且無語意衝突

## L. Website 功能對齊 Console
- [x] L1. 建立 Website 操作面板，覆蓋建立/刪除/搬移/改名（檔案與資料夾）【SP-FUNC-014】
- [x] L2. 建立 Website 上傳流程（檔案選擇 + 後端呼叫 + 結果回饋）【SP-FUNC-014】【SP-FUNC-017】
- [x] L3. 建立 Website 查詢面板（Search/Size/XML/Feature Flags）【SP-FUNC-014】
- [x] L4. 對齊 Website 與 Console 的成功/失敗語意與訊息格式【SP-FUNC-015】【SP-NFR-018】
- [x] L5. 補齊 `FileSystemApiClient` 對應方法與錯誤處理（含必要 DTO mapping）【SP-FUNC-015】
- [x] L6. 若現行 API 不足，調整 `WebApi` 契約以支持 Website 瀏覽器情境（不破壞核心流程）【SP-FUNC-016】
- [x] L7. 補齊 Website 端操作的整合測試/最小驗證腳本【SP-FUNC-018】
- [x] L8. 更新 README 與驗收文件，明確列示 Website 功能覆蓋範圍【SP-FUNC-018】
- [x] L9. 驗收 AC-023：Website 與 Console 功能語意一致且可重現
