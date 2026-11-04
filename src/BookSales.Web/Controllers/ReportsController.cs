using BookSales.Application.Reports;
using Microsoft.AspNetCore.Mvc;
namespace BookSales.Web.Controllers;

[Route("reports/inventory")]
public sealed class ReportsController(InventoryReportService service) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, bool inStockOnly, CancellationToken ct)
    {
        if (search?.Length > 200) return BadRequest("Search is limited to 200 characters.");
        ViewBag.Search = search; ViewBag.InStockOnly = inStockOnly;
        return View(await service.GenerateAsync(new(search, inStockOnly), ct));
    }
    [HttpGet("export")]
    public async Task<IActionResult> Export(string? search, bool inStockOnly, CancellationToken ct)
    {
        if (search?.Length > 200) return BadRequest("Search is limited to 200 characters.");
        var report = await service.GenerateAsync(new(search, inStockOnly), ct);
        return File(InventoryCsv.Export(report), "text/csv; charset=utf-8", "inventory.csv");
    }
}
