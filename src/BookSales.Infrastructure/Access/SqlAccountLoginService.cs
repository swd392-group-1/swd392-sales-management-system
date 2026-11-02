using BookSales.Application.Access;
using BookSales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookSales.Infrastructure.Access;

public sealed class SqlAccountLoginService(
    BookSalesDbContext db,
    IPasswordHashVerifier verifier) : IAccountLoginService
{
    public async Task<VerifiedAccount?> VerifyAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return null;

        var account = await db.Accounts.AsNoTracking()
            .Where(a => a.Username == username && a.Status == "ACTIVE")
            .Select(a => new { a.AccountId, a.Username, a.PasswordHash })
            .SingleOrDefaultAsync(cancellationToken);

        if (account is null || string.IsNullOrWhiteSpace(account.PasswordHash))
            return null;

        return verifier.Verify(account.PasswordHash, password)
            ? new VerifiedAccount(account.AccountId, account.Username)
            : null;
    }
}
