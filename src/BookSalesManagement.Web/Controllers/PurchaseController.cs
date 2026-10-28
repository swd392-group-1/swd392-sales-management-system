using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BookSalesManagement.Web.Controllers;

public sealed class PurchaseController : Controller
{
    private readonly IPurchaseQueryService _purchase;

    public PurchaseController(IPurchaseQueryService purchase)
    {
        _purchase = purchase;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var orders = await _purchase.GetPurchaseOrdersAsync(cancellationToken);
        var receipts = await _purchase.GetGoodsReceiptsAsync(cancellationToken);
        return View(new PurchaseIndexViewModel(orders, receipts));
    }
}
