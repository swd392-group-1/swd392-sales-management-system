using BookSales.Application.DTOs;
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
        public async Task<IActionResult> UpdatePrice(
            string bookId,
            [FromBody] UpdatePriceRequest request)
        {
            try
            {
                var success = await _pricingService
                    .UpdateCurrentPriceAsync(bookId, request.NewPrice);

                if (!success)
                {
                    return NotFound(new
                    {
                        Message = "Book not found."
                    });
                }

                return Ok(new
                {
                    Message = "Price updated successfully."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
        }
    }
}
