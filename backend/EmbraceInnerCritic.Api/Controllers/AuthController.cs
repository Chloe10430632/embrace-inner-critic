using System.Security.Claims;
using EmbraceInnerCritic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EmbraceInnerCritic.Api.Controllers;


//密封類別 (Sealed Class)：當類別加上 sealed 時，其他類別無法再繼承它。
//這能保護類別的內部實作不被子類別破壞或非預期修改。
[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        // 根據請求附帶的登入 Cookie，找出目前登入的本站使用者。
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            await signInManager.SignOutAsync();
            return Unauthorized();
        }

        return Ok(new
        {
            id = user.Id,
            email = user.Email
        });
    }

    [HttpGet("google")]
    public IActionResult Google()
    {
        if (string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]))
        {
            return Problem(
                title: "Google 登入尚未設定",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        // Google 驗證完成後，最後要回到本站的 google-response。
        var redirectUrl = Url.ActionLink(nameof(GoogleResponse), values: null)
            ?? throw new InvalidOperationException("無法建立 Google 登入回呼網址。");

        // 放外部登入需要的資訊，例如完成後的回跳位置
        var properties = signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
        // 交給 ASP.NET Core 的 Google 驗證流程，瀏覽器會被導向 Google。
        return Challenge(properties, "Google");
    }

    [HttpGet("google-response")]
    public async Task<IActionResult> GoogleResponse()
    {
        var frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
        // 讀取 Google 驗證後暫存的外部登入資料；失敗就回前端顯示失敗。
        var loginInfo = await signInManager.GetExternalLoginInfoAsync();
        if (loginInfo is null)
        {
            logger.LogWarning("Google callback did not contain external login information.");
            return Redirect(frontendBaseUrl + "/?login=failed");
        }

        // 已綁定 Google 帳號的本站使用者，可直接登入並取得本站的登入 Cookie。
        var signInResult = await signInManager.ExternalLoginSignInAsync(
            loginInfo.LoginProvider,
            loginInfo.ProviderKey,
            isPersistent: true);

        if (!signInResult.Succeeded)
        {
            // 第一次用這個 Google 帳號登入：取得 email，建立本站帳號並綁定 Google 登入。
            var email = loginInfo.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogWarning("Google callback did not contain an email claim.");
                return Redirect(frontendBaseUrl + "/?login=failed");
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                logger.LogWarning(
                    "Could not create the Google user. Identity errors: {Errors}",
                    string.Join(", ", createResult.Errors.Select(error => error.Code)));
                return Redirect(frontendBaseUrl + "/?login=failed");
            }

            var loginResult = await userManager.AddLoginAsync(user, loginInfo);
            if (!loginResult.Succeeded)
            {
                logger.LogWarning(
                    "Could not link the Google login. Identity errors: {Errors}",
                    string.Join(", ", loginResult.Errors.Select(error => error.Code)));
                return Redirect(frontendBaseUrl + "/?login=failed");
            }

            // 新帳號也要登入，讓瀏覽器收到本站的登入 Cookie。
            await signInManager.SignInAsync(user, isPersistent: true);
        }

        // 登入完成後回到前端；前端再呼叫 /api/auth/me 確認使用者。
        return Redirect(frontendBaseUrl + "/journals?login=success");
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // 清除本站登入 Cookie，結束這次登入狀態。
        await signInManager.SignOutAsync();
        return NoContent();
    }
}
