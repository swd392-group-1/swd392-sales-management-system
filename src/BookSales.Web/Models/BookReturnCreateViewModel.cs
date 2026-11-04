using System.ComponentModel.DataAnnotations;
using BookSales.Application.Returns;

namespace BookSales.Web.Models;

public sealed class BookReturnCreateViewModel
{
    [Required]
    [StringLength(200)]
    public string ReturnId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string OriginalOrderId { get; set; } = string.Empty;

    public ReturnableOrderDetails? Order { get; set; }
    public bool IsBookNewConfirmed { get; set; }
    public bool IsCashRefundConfirmed { get; set; }
    public List<BookReturnLineInput> Lines { get; set; } = new();
    public string? Error { get; set; }
}

public sealed class BookReturnLineInput
{
    public long OriginalOrderLineId { get; set; }
    public decimal ReturnQuantity { get; set; }
}
