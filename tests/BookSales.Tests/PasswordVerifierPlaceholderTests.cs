using BookSales.Infrastructure.Access;

namespace BookSales.Tests;

public sealed class PasswordVerifierPlaceholderTests
{
    [Fact]
    public void UnconfiguredVerifier_AlwaysDenies()
    {
        var verifier = new UnconfiguredPasswordHashVerifier();
        Assert.False(verifier.Verify("some-hash", "some-password"));
        Assert.False(verifier.Verify("", ""));
    }
}
