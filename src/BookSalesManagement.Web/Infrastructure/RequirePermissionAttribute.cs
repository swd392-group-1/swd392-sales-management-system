using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BookSalesManagement.Web.Infrastructure;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute, IAsyncActionFilter
{
    private readonly string[] _permissionIds;

    public RequirePermissionAttribute(params string[] permissionIds)
    {
        _permissionIds = permissionIds;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var currentUser = services.GetRequiredService<ICurrentUser>();
        var checker = services.GetRequiredService<IPermissionChecker>();

        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.EmployeeId))
        {
            context.Result = new RedirectToActionResult(
                "Index",
                "Session",
                new { returnUrl = context.HttpContext.Request.Path.ToString() });
            return;
        }

        foreach (var permissionId in _permissionIds)
        {
            if (await checker.HasPermissionAsync(currentUser.EmployeeId, permissionId, context.HttpContext.RequestAborted))
            {
                await next();
                return;
            }
        }

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), context.ModelState)
        {
            ["PermissionId"] = _permissionIds[0],
            ["PermissionName"] = string.Join(" hoặc ", _permissionIds.Select(PermissionCodes.Describe)),
            ["CurrentUser"] = currentUser
        };

        context.Result = new ViewResult
        {
            ViewName = "Forbidden",
            StatusCode = StatusCodes.Status403Forbidden,
            ViewData = viewData
        };
    }
}
