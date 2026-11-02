
namespace BookSales.Domain.Access;

public sealed class Account
{
    public string AccountId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? PasswordHash { get; set; }
}
