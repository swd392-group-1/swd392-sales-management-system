namespace BookSales.Application.Access;

public sealed record VerifiedAccount(string AccountId, string Username);

public interface IAccountLoginService
{
    Task<VerifiedAccount?> VerifyAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
