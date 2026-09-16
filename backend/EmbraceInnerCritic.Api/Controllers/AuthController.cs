using System.Security.Claims;
using EmbraceInnerCritic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EmbraceInnerCritic.Api.Controllers;

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

        var redirectUrl = Url.ActionLink(nameof(GoogleResponse), values: null)
            ?? throw new InvalidOperationException("無法建立 Google 登入回呼網址。");
        var properties = signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
        return Challenge(properties, "Google");
    }

    [HttpGet("google-response")]
    public async Task<IActionResult> GoogleResponse()
    {
        var frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
        var loginInfo = await signInManager.GetExternalLoginInfoAsync();
        if (loginInfo is null)
        {
            logger.LogWarning("Google callback did not contain external login information.");
            return Redirect(frontendBaseUrl + "/?login=failed");
        }

        var signInResult = await signInManager.ExternalLoginSignInAsync(
            loginInfo.LoginProvider,
            loginInfo.ProviderKey,
            isPersistent: true);

        if (!signInResult.Succeeded)
        {
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

            await signInManager.SignInAsync(user, isPersistent: true);
        }

        return Redirect(frontendBaseUrl + "/?login=success&next=journals");
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }
}
