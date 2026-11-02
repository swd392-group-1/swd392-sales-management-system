using System.Security.Claims;
using BookSales.Application.Access;
using BookSales.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookSales.Web.Controllers;

[Route("access")]
public sealed class AuthController(
    IAccountLoginService logins,
    IAccessLogWriter audit) : Controller
{
    private string Ip => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null) =>
        View(new AccessLoginInput { ReturnUrl = returnUrl });

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AccessLoginInput input,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(input);

        var account = await logins.VerifyAsync(
            input.Username, input.Password, cancellationToken);

        // Log before issuing the cookie. With no configured hash verifier,
        // every login is intentionally denied and still logged.
        await audit.WriteAsync(account?.AccountId, "/access/login",
            account is not null, Ip, cancellationToken);

        if (account is null)
        {
            ModelState.AddModelError(string.Empty, "Đăng nhập không thành công.");
            return View(input);
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, account.AccountId),
                new Claim(ClaimTypes.Name, account.Username)
            ], CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return !string.IsNullOrEmpty(input.ReturnUrl) && Url.IsLocalUrl(input.ReturnUrl)
            ? LocalRedirect(input.ReturnUrl)
            : RedirectToAction("Index", "Home")!;
    }

    [AllowAnonymous]
    [HttpGet("denied")]
    public IActionResult Denied() => StatusCode(403, "Không có quyền truy cập.");

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await audit.WriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
            "/access/logout", true, Ip, cancellationToken);
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home")!;
    }
}
