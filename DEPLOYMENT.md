# Staging 部署

測試環境使用單一 Render Web Service：ASP.NET Core 提供 API，也提供 Nuxt 產生的靜態前端；資料庫沿用 Supabase PostgreSQL。前後端共用同一個 HTTPS 網址，因此登入 Cookie 與 CSRF token 不需要跨站傳送。

## 建立 Render 服務

1. 登入 Render，選擇 **New > Blueprint**。
2. 連接 GitHub repository `embrace-inner-critic`。
3. Render 會讀取根目錄的 `render.yaml`，建立 `embrace-inner-critic-staging` Web Service。
4. 在建立畫面輸入以下三個 Secret：
   - `ConnectionStrings__DefaultConnection`
   - `Authentication__Google__ClientId`
   - `Authentication__Google__ClientSecret`
5. 等待部署完成，確認 `/health` 回傳 `{ "status": "ok" }`。

不要將 Secret 寫入 `render.yaml`、`.env` 或 Git repository。

如果 Render 因名稱衝突產生不同網址，須同步修改：

- Render 的 `Frontend__BaseUrl`
- Render 的 `AllowedHosts`
- Google OAuth 的 redirect URI

## Google OAuth 測試設定

在 Google Cloud Console 的 OAuth 用戶端加入：

```text
https://embrace-inner-critic-staging.onrender.com/signin-google
```

OAuth consent screen 維持 Testing，並只加入需要測試的 Google 帳號。不要把測試站當作公開服務。

## 部署後檢查

1. 開啟首頁，確認 CSS、插圖與音樂檔可載入。
2. 開啟 `/health`，確認 API 存活。
3. 使用允許的 Google 帳號登入及登出。
4. 建立一筆不含真實敏感內容的測試日記。
5. 重新整理後確認日記仍存在，再測試修改與刪除。
6. 使用另一個測試帳號，確認看不到前一個帳號的日記。

Render Free Web Service 閒置後會休眠，第一次開啟可能需要等待約一分鐘。這個環境只供功能驗證，不應儲存無法接受外洩的真實日記。
