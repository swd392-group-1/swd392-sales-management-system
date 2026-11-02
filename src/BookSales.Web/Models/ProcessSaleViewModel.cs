using System.ComponentModel.DataAnnotations;
using BookSales.Application.Sales;

namespace BookSales.Web.Models;

public sealed class ProcessSaleViewModel
{
    [Required]
    public string OrderId { get; set; } = string.Empty;

    [StringLength(200)]
    public string? CustomerId { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = "Cash";

    public List<ProcessSaleLineInput> Lines { get; set; } = new();
    public IReadOnlyList<SaleBookOption> Books { get; set; } = Array.Empty<SaleBookOption>();
    public ProcessSaleResult? Result { get; set; }
    public string? Error { get; set; }
}

public sealed class ProcessSaleLineInput
{
    public string BookId { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}
