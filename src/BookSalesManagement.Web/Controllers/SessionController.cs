using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Web.Infrastructure;
using BookSalesManagement.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BookSalesManagement.Web.Controllers;

public sealed class SessionController : Controller
{
    private readonly IAccountDirectory _accounts;
    private readonly ICurrentUser _currentUser;

    public SessionController(IAccountDirectory accounts, ICurrentUser currentUser)
    {
        _accounts = accounts;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var accounts = await _accounts.ListAsync(cancellationToken);
        return View(new SessionViewModel(accounts, _currentUser.AccountId));
    }

    [HttpPost]
    public async Task<IActionResult> Select(string accountId, string? returnUrl, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindAsync(accountId, cancellationToken);
        if (account is null)
        {
            TempData["Error"] = "Tài khoản demo không tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        Response.Cookies.Append(
            DemoSessionMiddleware.CookieName,
            account.AccountId,
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(7)
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Reset()
    {
        Response.Cookies.Delete(DemoSessionMiddleware.CookieName);
        return RedirectToAction(nameof(Index));
    }
}
