
using BookSales.Application.Access;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BookSales.Web.Access;

public sealed class IdentityPasswordHashVerifier
    : IPasswordHashVerifier
{
    private readonly PasswordHasher<object> _hasher =
        new(Options.Create(new PasswordHasherOptions()));

    public bool Verify(string storedHash, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(storedHash) ||
            string.IsNullOrEmpty(providedPassword))
        {
            return false;
        }

        try
        {
            var result = _hasher.VerifyHashedPassword(
                new object(),
                storedHash,
                providedPassword);

            return result != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
