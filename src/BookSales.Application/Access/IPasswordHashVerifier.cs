namespace BookSales.Application.Access;

public interface IPasswordHashVerifier
{
    bool Verify(string storedHash, string providedPassword);
}
