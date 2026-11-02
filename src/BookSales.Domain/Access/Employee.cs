
namespace BookSales.Domain.Access;

public sealed class Employee
{
    public string EmployeeId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string RoleCode { get; set; } = string.Empty;
}
