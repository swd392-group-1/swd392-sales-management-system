using System.ComponentModel.DataAnnotations;

namespace BookSales.Web.Models;

public sealed class StaffPermissionInput
{
    [Required]
    public string TargetEmployeeId { get; set; } = string.Empty;

    [Required]
    public string PermissionId { get; set; } = string.Empty;

    public bool Grant { get; set; }
}
