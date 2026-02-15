# 雲端檔案管理系統部署任務清單（Docker / Deployment）

> 使用方式：完成一項即勾選。
>
> 本清單僅包含「部署與交付」相關項目；`【SP-DEP-*】` 與 `AC-DEP-*` 為追蹤編號。

## A. 部署前技術凍結
- [ ] A1. 確認本次部署範圍：`WebApi + Website`（MVP）【SP-DEP-001】
- [ ] A2. 確認容器編排工具：`docker compose`（v2）【SP-DEP-002】
- [ ] A3. 確認映像建置策略：開發與正式環境可分離（dev/prod）【SP-DEP-003】
- [ ] A4. 確認資料持久化策略（Volume / Bind Mount）【SP-DEP-004】
- [ ] A5. 確認設定來源：環境變數優先於 appsettings【SP-DEP-005】

## B. 映像與 Dockerfile
- [ ] B1. 為 `WebApi` 建立 Dockerfile（建置階段 + 執行階段）【SP-DEP-006】
- [ ] B2. 為 `Website` 建立 Dockerfile（靜態站點或前端服務）【SP-DEP-007】
- [ ] B3. 設定 `.dockerignore`（排除 bin/obj/node_modules 等）【SP-DEP-008】
- [ ] B4. 映像標籤策略（`app:version`、`app:latest`）【SP-DEP-009】
- [ ] B5. 映像安全最小化（非 root、最小基底映像）【SP-DEP-010】

## C. Compose 編排
- [ ] C1. 建立 `docker-compose.yml`（api、web）【SP-DEP-011】
- [ ] C2. 規劃網路與服務名稱（內部 DNS）【SP-DEP-012】
- [ ] C3. 規劃埠對映（Host:Container）與避免衝突【SP-DEP-013】
- [ ] C4. 設定 `depends_on` 與啟動順序【SP-DEP-014】
- [ ] C5. 規劃 profile（例如 `dev`、`prod`）【SP-DEP-015】

## D. 設定與密鑰管理
- [ ] D1. 定義必要環境變數清單與預設值【SP-DEP-016】
- [ ] D2. 提供部署環境變數文件（不含敏感資訊）【SP-DEP-017】
- [ ] D3. 連線字串與路徑改由環境變數注入【SP-DEP-018】
- [ ] D4. 敏感值不得寫入 Git（Token/Password/Key）【SP-DEP-019】
- [ ] D5. 啟動時輸出關鍵設定檢查摘要（遮罩敏感值）【SP-DEP-020】

## E. 儲存與資料策略
- [ ] E1. 規劃 `StorageRootPath` 對應容器掛載路徑【SP-DEP-021】
- [ ] E2. 驗證容器重啟後資料仍存在【SP-DEP-022】
- [ ] E3. 規劃輸出目錄（如 XML 檔）掛載策略【SP-DEP-023】
- [ ] E4. 定義資料初始化行為（首次啟動/重啟）【SP-DEP-024】
- [ ] E5. 文件化清理策略（何時可刪 volume）【SP-DEP-025】

## F. 健康檢查與可觀測性
- [ ] F1. `WebApi` 加入 health endpoint（例：`/health`）【SP-DEP-026】
- [ ] F2. Compose healthcheck 與重試策略【SP-DEP-027】
- [ ] F3. 統一容器日誌輸出格式（至少含時間與等級）【SP-DEP-028】
- [ ] F4. 提供常用除錯指令（logs/ps/exec）【SP-DEP-029】
- [ ] F5. 定義最低可用驗證流程（API 可回應、網站可連線）【SP-DEP-030】

## G. CI/CD（可選，建議）
- [ ] G1. 建立 CI 工作流程：建置映像與基本測試【SP-DEP-031】
- [ ] G2. 針對主分支產生版本標籤與映像標記【SP-DEP-032】
- [ ] G3. 失敗時保留必要 artifact（log / test result）【SP-DEP-033】
- [ ] G4. 發佈流程加入手動核准節點（prod）【SP-DEP-034】
- [ ] G5. 補齊回滾策略（上一版映像快速回退）【SP-DEP-035】

## H. 部署驗證（AC-DEP）
- [ ] H1. AC-DEP-001：`docker compose up -d --build` 可成功啟動全部服務
- [ ] H2. AC-DEP-002：Website 可透過 WebApi 正常操作（無跨層直連）
- [ ] H3. AC-DEP-003：重啟容器後檔案資料仍可讀取（持久化有效）
- [ ] H4. AC-DEP-004：`/health` 狀態正常，healthcheck 通過
- [ ] H5. AC-DEP-005：關鍵設定可由環境變數覆蓋且行為正確
- [ ] H6. AC-DEP-006：部署文件可讓新成員在 30 分鐘內完成本機啟動

## I. 文件交付
- [ ] I1. 更新 `README.md`：本機部署步驟（Prerequisites / Up / Down）【SP-DEP-036】
- [ ] I2. 新增 `docs/deploy.md`：架構圖、變數表、常見錯誤排查【SP-DEP-037】
- [ ] I3. 補上版本相容矩陣（Docker / Compose / .NET）【SP-DEP-038】
- [ ] I4. 提供最小指令集（build、up、down、logs、reset）【SP-DEP-039】
- [ ] I5. 補上風險與限制（已知問題）【SP-DEP-040】
