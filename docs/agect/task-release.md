 # 雲端檔案管理系統交付任務清單（Release & Delivery）

> 使用方式：完成一項即勾選。
>
> 本清單聚焦交付、驗收、文件與收尾，不含程式細部開發步驟。

## A. 交付範圍確認
- [x] A1. 確認本次提交範圍為 MVP（F1~F5）
- [x] A2. 確認 Phase 2（管理操作）若未完成，不影響 MVP 交付判定
- [x] A3. 確認功能以 `MVP + Phase 2 / MMP` 分級交付，無 OOS/Bonus 清單

## B. 必要交付物
- [x] B1. UML Class Diagram 完成且可閱讀【SP-DEL-001】
- [x] B2. ER Model 完成且欄位清楚【SP-DEL-002】
- [x] B3. 可執行程式與執行指令說明完整【SP-DEL-003】
- [x] B4. Console、WebApi、Website 驗證輸出可重現【SP-DEL-004】
- [x] B5. README/設計說明文件完整【SP-DEL-005】
- [ ] B6. GitHub Repository 可公開檢視與下載【SP-DEL-006】
- [x] B7. UML 已明確呈現繼承/關聯/聚合或組合關係【SP-DEL-007】
- [x] B8. ER Model 可檢核 PK/FK、遞迴層級與屬性映射【SP-DEL-008】
- [x] B9. C# 專案可建置且可啟動 MVP 核心流程（F1~F5）【SP-DEL-009】
- [ ] B10. 驗收證據逐項對應 F1~F5（含命令、輸出片段、資料集版本）【SP-DEL-010】
- [ ] B11. README 最低必要欄位完整（環境/建置/執行/示範輸出/設計摘要/文件索引）【SP-DEL-011】
- [ ] B12. Repo 可由評分者存取且內容完整（需求/規格/模型/架構/程式/驗收）【SP-DEL-012】

## C. 驗收確認
- [x] C1. AC-001 通過：目錄樹建立與輸出正確
- [x] C2. AC-002 通過：總容量計算與單位規則正確
- [x] C3. AC-003 通過：副檔名搜尋結果與順序正確
- [x] C4. AC-004 通過：XML 輸出符合語意與層級
- [x] C5. AC-005 通過：三種介面皆可驗證 Traverse Log 遍歷過程
- [x] C6. AC-006 通過：儲存路徑配置跨環境可運作
- [x] C7. AC-007 通過：Logging/XML 輸出行為可切換
- [x] C8. AC-008 通過：副檔名分組規則可運作
- [x] C9. AC-009 通過：設定版本與相容性策略可運作
- [x] C10. AC-010 通過：擴充機制與功能旗標可運作
- [x] C11. AC-011 通過：分層與維護性要求落實
- [x] C12. AC-012 通過：雙表現層分工與依賴邊界正確
- [x] C12a. AC-013 通過：實體儲存與資料庫中繼資料一致
- [x] C12b. AC-014 通過：SQLite 預設執行與 SQL Server 相容切換成立
- [x] C12c. AC-015 通過：Provider 設定合法性與啟動失敗機制完整
- [x] C12d. AC-016 通過：Provider 切換不破壞對外契約邊界
- [x] C12e. AC-017 通過：Code First 與 Migration 版本化成立
- [x] C12f. AC-018 通過：自動檢查與 Migration 策略可穩定運作
- [x] C13. AC-019 通過：三專案分層與 DI 組裝邊界符合規範
- [x] C14. AC-020 通過：Domain 可獨立打包且契約拆分策略具可控觸發條件
- [x] C15. AC-021 通過：ER Schema 定案與資料一致性約束已正確落地
- [x] C15a. AC-022 通過：`WebApi/Website` 設定分層責任清楚且無語意衝突
- [ ] C15b. AC-023 通過：`Website` 已對齊 `Console` 可操作能力與行為語意
- [x] C15c. AC-024 通過：初始化資料集深度與建立順序可驗證
- [ ] C15c1. 完成 Root 與至少 1 個中介層目錄之人工換算與程式輸出比對證據【SP-FUNC-026】
- [ ] C15d. AC-025 通過：容量驗收人工換算證據完整
- [x] C15e. AC-026 通過：無命中搜尋行為正確且穩定
- [x] C15f. AC-027 通過：XML 輸出結構合法可解析
- [ ] C15g. AC-028 通過：文件與交付物細節完整
- [ ] C15h. AC-029 通過：`spec` 已完整覆蓋 `mvp_ac` 並具可追溯證據
- [ ] C16. AC-DEP-001 通過：容器化服務可建置並成功啟動
- [ ] C17. AC-DEP-002 通過：Website 經 WebApi 操作之依賴邊界正確
- [ ] C18. AC-DEP-003 通過：資料持久化策略可運作
- [ ] C19. AC-DEP-004 通過：健康檢查與最小可用驗證到位
- [ ] C20. AC-DEP-005 通過：設定覆蓋策略可預期
- [ ] C21. AC-DEP-006 通過：部署文件可支援新成員快速啟動

## D. 文件一致性
- [x] D1. `docs/spec.md` 與實作行為一致（命名、流程、預設值）
- [x] D2. `appsettings.json` 範例與實際可用設定一致
- [x] D3. `docs/task.md`、`docs/task-dev.md`、`docs/task-release.md`、`docs/task-deploy.md` 內容一致
- [x] D4. README 的執行範例與實際輸出一致
- [x] D5. `docs/config-boundary-checklist.md` 與 `WebApi/Website` 設定責任一致
- [ ] D6. `docs/mvp_ac.md` 已補齊 AC-023 驗收案例且可重現
- [ ] D7. `docs/mvp_ac.md` 已補齊 AC-024~AC-029 驗收案例且可重現

## E. 範圍一致性
- [x] E1. 確認文件不再使用 OOS/Bonus 清單；功能以 `MVP + Phase 2 / MMP` 分級管理
- [x] E2. 確認正式功能包含管理操作（Upload/Download/Move/Rename/Delete）並與規格一致【SP-MGMT-001~SP-MGMT-010】
- [x] E3. 確認驗收敘述與分級一致：MVP 基線與 Phase 2 路線圖無語意衝突

## F. 最終收尾
- [ ] F1. 版本標記與提交訊息清楚（可追溯）
- [x] F2. 文件無過期描述（例如舊路徑、舊設定鍵）
- [x] F3. 最終走查：命名、型別、安全性、可讀性、可維護性
