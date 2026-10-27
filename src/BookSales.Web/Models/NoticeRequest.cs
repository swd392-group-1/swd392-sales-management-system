using System.ComponentModel.DataAnnotations;

namespace BookSales.Web.Models;

public sealed class NoticeRequest
{
    [Required(ErrorMessage = "Nhập nội dung kiểm tra.")]
    [StringLength(200, ErrorMessage = "Nội dung kiểm tra tối đa 200 ký tự.")]
    public string Message { get; init; } = "";
}
