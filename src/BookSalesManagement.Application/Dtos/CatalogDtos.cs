namespace BookSalesManagement.Application.Dtos;

public sealed record BookOption(string Id, string Title, decimal CurrentQuantity);

public sealed record DemoAccount(
    string AccountId,
    string EmployeeId,
    string Username,
    string EmployeeName,
    string RoleCode,
    string Status,
    bool IsActive);
