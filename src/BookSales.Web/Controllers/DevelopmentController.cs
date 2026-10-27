using System.Net;
using BookSales.Application.Notifications;
using BookSales.Infrastructure.Persistence;
using BookSales.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookSales.Web.Controllers;

[Route("development")]
public sealed class DevelopmentController(IWebHostEnvironment environment) : Controller
{
    private bool IsLocalDevelopment => environment.IsDevelopment()
        && HttpContext.Connection.RemoteIpAddress is { } address
        && IPAddress.IsLoopback(address);

    [HttpGet("")]
    public IActionResult Index() => IsLocalDevelopment ? View() : NotFound();

    [HttpPost("notices")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        [FromBody] NoticeRequest? request,
        [FromServices] DevelopmentNoticeService service,
        CancellationToken cancellationToken)
    {
        if (!IsLocalDevelopment) return NotFound();
        if (request is null || string.IsNullOrWhiteSpace(request.Message) || !ModelState.IsValid)
        {
            return BadRequest(new { message = "Nội dung kiểm tra cần có từ 1 đến 200 ký tự." });
        }
        var notice = await service.SubmitAsync(request.Message, cancellationToken);
        return Accepted(new { notice.Id, state = "queued" });
    }

    [HttpGet("database")]
    public async Task<IActionResult> Database(CancellationToken cancellationToken)
    {
        if (!IsLocalDevelopment) return NotFound();
        var database = HttpContext.RequestServices.GetService<BookSalesDbContext>();
        if (database is null) return Ok(new { configured = false, connected = false });
        var connected = await database.Database.CanConnectAsync(cancellationToken);
        return Ok(new { configured = true, connected });
    }
}
