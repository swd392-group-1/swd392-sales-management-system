namespace BookSalesManagement.Web.Models.Common;

public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
