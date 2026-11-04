using System.ComponentModel.DataAnnotations;
using BookSales.Application.Returns;

namespace BookSales.Web.Models;

public sealed class BookReturnSearchViewModel
{
    [StringLength(200)]
    public string? OrderId { get; set; }

    public ReturnableOrderDetails? Order { get; set; }
    public string? Error { get; set; }
}
