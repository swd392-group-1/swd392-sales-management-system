using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Dtos;

namespace BookSalesManagement.Web.Infrastructure;

public sealed class DemoSessionMiddleware
{
    public const string CookieName = "swd392_demo_account";
    public const string DefaultAccountId = "DEMO-20261027-ACC-INVENTORY";
    public const string ItemKey = "SWD392.DemoAccount";

    private readonly RequestDelegate _next;

    public DemoSessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAccountDirectory accounts)
    {
        var accountId = context.Request.Cookies[CookieName];
        DemoAccount? account = null;

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            account = await accounts.FindAsync(accountId, context.RequestAborted);
        }

        if (account is null)
        {
            account = await accounts.FindAsync(DefaultAccountId, context.RequestAborted);
        }

        context.Items[ItemKey] = account;
        await _next(context);
    }
}
