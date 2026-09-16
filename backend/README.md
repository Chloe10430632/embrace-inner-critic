# 後端設定

此 API 提供 Google 登入與登入後的日記 CRUD。瀏覽器不會直接連 Supabase；API 使用 ASP.NET Core Identity 驗證使用者，並以使用者 ID 篩選每一筆日記。

不要把連線字串或 Google Client Secret 加入版本控制。先進入 backend/EmbraceInnerCritic.Api，然後以 User Secrets 設定：

    dotnet user-secrets init
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "你的 Supabase PostgreSQL 連線字串"
    dotnet user-secrets set "Authentication:Google:ClientId" "你的 Google OAuth Client ID"
    dotnet user-secrets set "Authentication:Google:ClientSecret" "你的 Google OAuth Client Secret"

在 Google Cloud Console 的 OAuth 用戶端中，加入授權回呼 URI：

    https://localhost:7228/signin-google

設定完成後，在同一個資料夾建立 migration 與資料表，最後啟動 API：

    dotnet ef migrations add InitialIdentity
    dotnet ef database update
    dotnet run

前端預設在 http://localhost:3000，後端開發 HTTPS 位址為 https://localhost:7228。
