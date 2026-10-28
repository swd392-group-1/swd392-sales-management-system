using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Dtos;

namespace BookSalesManagement.Web.Infrastructure;

public sealed class DemoCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DemoCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private DemoAccount? Account
        => _httpContextAccessor.HttpContext?.Items[DemoSessionMiddleware.ItemKey] as DemoAccount;

    public bool IsAuthenticated => Account is not null;

    public string? AccountId => Account?.AccountId;

    public string? EmployeeId => Account?.EmployeeId;

    public string DisplayName => Account?.EmployeeName ?? "Chưa chọn tài khoản";

    public string? RoleCode => Account?.RoleCode;
}
