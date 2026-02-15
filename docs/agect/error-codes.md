# Error Codes

本文件說明 CloudFileManager 的操作錯誤碼（`OperationResultDto.ErrorCode`），用於：

- 前端/客戶端做穩定分流（不要依賴訊息字串）。
- 觀測與告警聚合（以錯誤碼統計）。
- API 相容演進（訊息可調整，錯誤碼盡量穩定）。

## 回傳規則

- 成功操作：`Success=true`，`ErrorCode=null`。
- 失敗操作：`Success=false`，`ErrorCode` 盡量帶值。
- `Message` 提供人類可讀描述；機器判斷請使用 `ErrorCode`。

## 錯誤碼清單

| ErrorCode | 說明 | 常見來源 | HTTP 建議 |
| --- | --- | --- | --- |
| `CFM_UNEXPECTED_ERROR` | 未分類的非預期錯誤 | Upload fallback / API fallback | `500`（若可判定為輸入則 `400`） |
| `CFM_UPLOAD_IO_ERROR` | 上傳檔案 I/O 失敗 | WebApi upload form | `400` |
| `CFM_UPLOAD_PERMISSION_DENIED` | 上傳檔案權限不足 | WebApi upload form | `403` |
| `CFM_UPLOAD_INVALID_REQUEST` | 上傳請求不合法（缺檔、狀態錯誤） | WebApi upload form | `400` |
| `CFM_UPLOAD_METADATA_SAVE_FAILED` | 檔案上傳後 metadata 寫入失敗 | Infrastructure upload | `500` |
| `CFM_MOVE_FILE_UNEXPECTED` | 檔案搬移流程非預期失敗（含 rollback 路徑） | Application/Infrastructure move file | `500` |
| `CFM_RENAME_FILE_UNEXPECTED` | 檔案改名流程非預期失敗（含 rollback 路徑） | Application/Infrastructure rename file | `500` |
| `CFM_MOVE_DIRECTORY_UNEXPECTED` | 目錄搬移流程非預期失敗（含 rollback 路徑） | Application/Infrastructure move dir | `500` |
| `CFM_RENAME_DIRECTORY_UNEXPECTED` | 目錄改名流程非預期失敗（含 rollback 路徑） | Infrastructure rename dir | `500` |
| `CFM_CREATE_DIRECTORY_UNEXPECTED` | 建立目錄流程非預期失敗 | Application create dir | `500` |
| `CFM_DIRECTORY_TREE_NETWORK_ERROR` | 讀取目錄樹網路失敗（預留） | Website view-model 組裝 | `503` |
| `CFM_DIRECTORY_TREE_TIMEOUT` | 讀取目錄樹逾時（預留） | Website view-model 組裝 | `504` |
| `CFM_DIRECTORY_TREE_UNEXPECTED` | 讀取目錄樹非預期失敗（預留） | Website view-model 組裝 | `500` |
| `CFM_COMMAND_EXECUTION_UNEXPECTED` | Console 命令執行非預期失敗（預留） | Console loop | N/A |

## 客戶端使用建議

- 以 `ErrorCode` 做條件分支，避免以 `Message` 字串比對。
- 對 `CFM_UNEXPECTED_ERROR` 類別顯示通用錯誤提示，並附 request id。
- 對可恢復錯誤（例如 `CFM_UPLOAD_IO_ERROR`）提供重試或重新選檔。

## 維護準則

- 新增錯誤碼請集中於 `src/CloudFileManager.Shared/Common/OperationErrorCodes.cs`。
- 錯誤碼命名採 `CFM_<CONTEXT>_<REASON>`（全大寫底線）。
- 優先保持向後相容：既有錯誤碼不要任意改名或重用不同語意。
