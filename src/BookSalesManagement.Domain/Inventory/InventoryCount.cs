using BookSalesManagement.Domain.Common;

namespace BookSalesManagement.Domain.Inventory;

public sealed class InventoryCount
{
    private InventoryCount()
    {
        Id = string.Empty;
        RecordedByEmployeeId = string.Empty;
    }

    public InventoryCount(string id, DateTime countedAtUtc, string recordedByEmployeeId)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new DomainException("Mã phiếu kiểm kê không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(recordedByEmployeeId))
        {
            throw new DomainException("Phiếu kiểm kê phải có nhân viên thực hiện.");
        }

        Id = id;
        CountedAtUtc = countedAtUtc;
        RecordedByEmployeeId = recordedByEmployeeId;
    }

    public string Id { get; private set; }

    public DateTime CountedAtUtc { get; private set; }

    public string RecordedByEmployeeId { get; private set; }

    public List<InventoryCountObservation> Observations { get; private set; } = new();

    public List<InventoryCountSelection> Selections { get; private set; } = new();

    public bool IsFinalized => Selections.Count > 0;

    public InventoryCountObservation AddObservation(string bookId, decimal countedQuantity, DateTime observedAtUtc)
    {
        EnsureOpen();
        var observation = new InventoryCountObservation(0, Id, bookId, countedQuantity, observedAtUtc);
        Observations.Add(observation);
        return observation;
    }

    public CountFinalization Finalize(
        IReadOnlyDictionary<string, long> selectedObservationIds,
        IReadOnlyDictionary<string, decimal> referenceQuantities)
    {
        EnsureOpen();

        if (Observations.Count == 0)
        {
            throw new DomainException("Phiếu kiểm kê chưa có dòng đếm nào.");
        }

        if (selectedObservationIds.Count == 0)
        {
            throw new DomainException("Phải chọn kết quả đếm trước khi chốt phiếu.");
        }

        var lines = new List<FinalizedCountLine>();
        foreach (var bookId in Observations.Select(o => o.BookId).Distinct())
        {
            if (!selectedObservationIds.TryGetValue(bookId, out var observationId))
            {
                throw new DomainException($"Chưa chọn kết quả đếm cho sách {bookId}.");
            }

            var observation = Observations.FirstOrDefault(o => o.Id == observationId && o.BookId == bookId)
                ?? throw new DomainException($"Kết quả đếm {observationId} không thuộc sách {bookId} của phiếu này.");

            if (!referenceQuantities.TryGetValue(bookId, out var referenceQuantity))
            {
                throw new DomainException($"Không đọc được tồn tham chiếu của sách {bookId}.");
            }

            lines.Add(new FinalizedCountLine(bookId, observationId, referenceQuantity, observation.CountedQuantity));
        }

        foreach (var line in lines)
        {
            Selections.Add(new InventoryCountSelection(Id, line.BookId, line.ObservationId, line.ReferenceQuantity));
        }

        return new CountFinalization(lines);
    }

    private void EnsureOpen()
    {
        if (IsFinalized)
        {
            throw new DomainException("Phiếu kiểm kê đã được chốt. Không thể ghi thêm hoặc chốt lại.");
        }
    }
}
