using BookSalesManagement.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace BookSalesManagement.Web.Controllers;

public sealed class AdjustmentsController : Controller
{
    private readonly IInventoryQueryService _inventory;

    public AdjustmentsController(IInventoryQueryService inventory)
    {
        _inventory = inventory;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _inventory.GetAdjustmentsAsync(cancellationToken));
}
