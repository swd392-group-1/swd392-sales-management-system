
using BookSales.Web.Access;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BookSales.Tests;

public sealed class IdentityPasswordHashVerifierTests
{
    private readonly IdentityPasswordHashVerifier _verifier = new();

    private static string CreateHash(string password)
    {
        var hasher = new PasswordHasher<object>(
            Options.Create(new PasswordHasherOptions()));

        return hasher.HashPassword(new object(), password);
    }

    [Fact]
    public void CorrectPassword_ReturnsTrue()
    {
        var hash = CreateHash("DemoPassword123!");

        Assert.True(
            _verifier.Verify(hash, "DemoPassword123!"));
    }

    [Fact]
    public void WrongPassword_ReturnsFalse()
    {
        var hash = CreateHash("DemoPassword123!");

        Assert.False(
            _verifier.Verify(hash, "WrongPassword123!"));
    }

    [Fact]
    public void InvalidHash_ReturnsFalse()
    {
        Assert.False(
            _verifier.Verify("invalid-hash", "DemoPassword123!"));
    }
}
