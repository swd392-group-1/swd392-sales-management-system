using BookSales.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookSales.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookController : ControllerBase
    {
        private readonly PricingService _pricingService;

        public BookController(PricingService pricingService)
        {
            _pricingService = pricingService;
        }

        [HttpPost("{bookId}/price")]
        public async Task<IActionResult> UpdatePrice(string bookId, [FromBody] UpdatePriceRequest request)
        {
            var success = await _pricingService.UpdateCurrentPriceAsync(bookId, request.NewPrice);

            if (success) return Ok(new { Message = "Price updated successfully." });
            return BadRequest(new { Message = "Failed to update price. Book not found." });
        }
    }
}