using BookSales.Application.Services;
using Microsoft.AspNetCore.Mvc;
namespace BookSales.Web.Controllers;
public sealed class PricingController(PricingService service) : Controller
{
    [HttpGet] public IActionResult Index() => View();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string bookId, decimal newPrice)
    {
        if (!ModelState.IsValid) { ViewBag.Message = "Giá không hợp lệ."; return View(); }
        try { ViewBag.Message = await service.UpdateCurrentPriceAsync(bookId, newPrice) ? "Đã lưu giá và xếp hàng thông báo." : "Không tìm thấy sách."; }
        catch (ArgumentException e) { ViewBag.Message = e.Message; }
        return View();
    }
}
