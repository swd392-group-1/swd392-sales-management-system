using BookSales.Application.Access;

namespace BookSales.Infrastructure.Access;

// Deliberately fails closed until the team confirms the hash format.
public sealed class UnconfiguredPasswordHashVerifier : IPasswordHashVerifier
{
    public bool Verify(string storedHash, string providedPassword) => false;
}
