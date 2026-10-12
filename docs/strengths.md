# 看見我的力量

獨立於覺察日記與追蹤進展的私人經驗紀錄，入口位於主要導覽的「看見我的力量」（`/strengths`）。不變更六題日記與進展追蹤資料。

- 記錄具體事件（必填，最多 2000 字）、補充（選填，最多 4000 字）。
- 特質標籤選填，可多選與自訂；每筆最多 10 個，每個最多 24 字。
- 預設標籤涵蓋九個分類、70 個詞彙；支援分類瀏覽、跨分類搜尋與已選標籤移除。分類研究、適用範圍與來源見 [特質與能力標籤](strength-traits.md)。標籤種類沒有總額上限，既有自訂標籤保留原文。
- 支援修改、刪除前確認、依特質篩選與特質筆數長條回顧。
- 筆數代表相關紀錄數，不代表能力評分。沒有標籤仍可儲存。
- 保留輸入失敗時的內容，儲存／刪除時鎖定重複操作，成功後更新頁面資料。

## 資料與啟用

API：`/api/strength-entries`，沿用 Cookie 登入、CSRF filter 與既有 rate limit。每次讀取最多 50 筆、每帳號最多 500 筆，新增與刪除透過交易維護獨立額度；所有紀錄操作以登入者及資源 id 檢查權限。

EF migration：`20261007122400_AddStrengthEntries`，新增 `StrengthEntries` 表及 `AspNetUsers.StrengthEntryCount`，不修改日記紀錄。新表啟用 RLS、撤銷 PUBLIC／anon／authenticated 存取；授權仍由 ASP.NET Identity API 處理，後端應使用原有可繞過 RLS 的受保護資料庫角色。

部署前須取得目標資料庫變更授權，再套用 migration 並重啟後端。程式碼完成不表示遠端資料庫已套用。回退 migration 會刪除新功能的紀錄，需先備份；不影響既有日記資料。

驗證：前端 typecheck／build、後端測試包含 CRUD、資料隔離、CSRF、額度與輸入限制。SQLite 測試驗證 API 資料流程；PostgreSQL 的 RLS／角色權限仍需於目標環境套用後驗證。
