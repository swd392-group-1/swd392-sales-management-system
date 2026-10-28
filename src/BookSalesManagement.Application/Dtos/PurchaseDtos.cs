namespace BookSalesManagement.Application.Dtos;

public sealed record PurchaseOrderLineRow(
    long Id,
    string BookId,
    string BookTitle,
    decimal OrderedQuantity,
    decimal ReceivedQuantity);

public sealed record PurchaseOrderRow(
    string Id,
    DateOnly? OrderDate,
    string Status,
    IReadOnlyList<string> Providers,
    IReadOnlyList<PurchaseOrderLineRow> Lines);

public sealed record GoodsReceiptLineRow(
    string PurchaseOrderId,
    string BookId,
    string BookTitle,
    decimal ReceivedQuantity);

public sealed record GoodsReceiptRow(
    string Id,
    DateOnly? ReceiptDate,
    IReadOnlyList<GoodsReceiptLineRow> Lines);
