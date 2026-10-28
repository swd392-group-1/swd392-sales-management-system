namespace BookSalesManagement.Application.Dtos;

public sealed record NewCountLine(string BookId, decimal CountedQuantity);

public sealed record InventoryCountSummary(
    string Id,
    DateTime CountedAtUtc,
    string EmployeeName,
    int BookCount,
    int ObservationCount,
    bool IsFinalized,
    string? AdjustmentId);

public sealed record ObservationDto(long Id, decimal CountedQuantity, DateTime ObservedAtUtc);

public sealed record CountLineDetail(
    string BookId,
    string BookTitle,
    decimal CurrentQuantity,
    IReadOnlyList<ObservationDto> Observations,
    long? SelectedObservationId,
    decimal? RecordedQuantity,
    decimal? CountedQuantity,
    decimal? Difference);

public sealed record InventoryCountDetail(
    string Id,
    DateTime CountedAtUtc,
    string EmployeeId,
    string EmployeeName,
    bool IsFinalized,
    string? AdjustmentId,
    IReadOnlyList<CountLineDetail> Lines);

public sealed record FinalizedLineResult(
    string BookId,
    decimal ReferenceQuantity,
    decimal CountedQuantity,
    decimal Difference);

public sealed record FinalizeCountResult(
    string CountId,
    string? AdjustmentId,
    IReadOnlyList<FinalizedLineResult> Lines);

public sealed record StockOverviewRow(
    string BookId,
    string Title,
    decimal CurrentQuantity,
    decimal LedgerQuantity,
    decimal Difference);

public sealed record MovementRow(
    long Id,
    string BookId,
    string Title,
    decimal QuantityDelta,
    DateTime OccurredAtUtc,
    string PostingKey);

public sealed record AdjustmentLine(string BookId, string Title, decimal QuantityChange);

public sealed record AdjustmentSummary(
    string Id,
    DateOnly? AdjustmentDate,
    string? Reason,
    string? CountId,
    IReadOnlyList<AdjustmentLine> Lines);
