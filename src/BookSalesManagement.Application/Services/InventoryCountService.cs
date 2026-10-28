using System.Data;
using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Common;
using BookSalesManagement.Application.Dtos;
using BookSalesManagement.Domain.Common;
using BookSalesManagement.Domain.Inventory;

namespace BookSalesManagement.Application.Services;

public sealed class InventoryCountService
{
    private readonly IInventoryCountStore _counts;
    private readonly IStockStore _stock;
    private readonly IInventoryAdjustmentStore _adjustments;
    private readonly IInventoryMovementStore _movements;
    private readonly IBookDirectory _books;
    private readonly IEmployeeDirectory _employees;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public InventoryCountService(
        IInventoryCountStore counts,
        IStockStore stock,
        IInventoryAdjustmentStore adjustments,
        IInventoryMovementStore movements,
        IBookDirectory books,
        IEmployeeDirectory employees,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _counts = counts;
        _stock = stock;
        _adjustments = adjustments;
        _movements = movements;
        _books = books;
        _employees = employees;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<string> CreateAsync(IReadOnlyList<NewCountLine> lines, CancellationToken cancellationToken = default)
    {
        ValidateLines(lines);
        var employeeId = RequireEmployeeId();
        await EnsureBooksExistAsync(lines.Select(line => line.BookId), cancellationToken);

        var now = _clock.UtcNow;
        var count = new InventoryCount(IdFactory.New("CNT", now), now, employeeId);
        foreach (var line in lines)
        {
            count.AddObservation(line.BookId, line.CountedQuantity, now);
        }

        await _counts.AddAsync(count, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return count.Id;
    }

    public async Task AddObservationAsync(
        string countId,
        string bookId,
        decimal countedQuantity,
        CancellationToken cancellationToken = default)
    {
        var count = await _counts.FindAsync(countId, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy phiếu kiểm kê {countId}.");

        if (count.IsFinalized)
        {
            throw new DomainException("Phiếu kiểm kê đã chốt, không thể ghi thêm kết quả đếm.");
        }

        await EnsureBooksExistAsync(new[] { bookId }, cancellationToken);
        count.AddObservation(bookId, countedQuantity, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<FinalizeCountResult> FinalizeAsync(
        string countId,
        IReadOnlyDictionary<string, long> selectedObservationIds,
        CancellationToken cancellationToken = default)
    {
        if (selectedObservationIds.Count == 0)
        {
            throw new DomainException("Phải chọn kết quả đếm cho từng sách trước khi chốt phiếu.");
        }

        RequireEmployeeId();

        return await _unitOfWork.ExecuteAsync(
            token => FinalizeCoreAsync(countId, selectedObservationIds, token),
            cancellationToken);
    }

    private async Task<FinalizeCountResult> FinalizeCoreAsync(
        string countId,
        IReadOnlyDictionary<string, long> selectedObservationIds,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var count = await _counts.FindAsync(countId, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy phiếu kiểm kê {countId}.");

        if (count.IsFinalized)
        {
            throw new DomainException("Phiếu kiểm kê đã được chốt. Không ghi lại cùng chênh lệch lần hai.");
        }

        var bookIds = count.Observations.Select(observation => observation.BookId).Distinct().ToArray();
        if (bookIds.Length == 0)
        {
            throw new DomainException("Phiếu kiểm kê chưa có dòng đếm nào.");
        }

        await _stock.EnsureStockRowsAsync(bookIds, cancellationToken);
        var referenceQuantities = await _stock.GetQuantitiesAsync(bookIds, cancellationToken);
        var finalization = count.Finalize(selectedObservationIds, referenceQuantities);

        var now = _clock.UtcNow;
        var adjustment = InventoryAdjustment.CreateForCount(
            count.Id,
            IdFactory.New("ADJ", now),
            now,
            finalization.Lines);

        foreach (var line in finalization.Lines)
        {
            var applied = await _stock.TrySetQuantityAsync(
                line.BookId,
                line.ReferenceQuantity,
                line.CountedQuantity,
                cancellationToken);

            if (!applied)
            {
                throw new ConflictException(
                    $"Tồn của sách {line.BookId} đã thay đổi trong lúc chốt phiếu. Hãy tải lại và kiểm kê lại.");
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (adjustment is not null)
        {
            await _adjustments.AddAsync(adjustment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _adjustments.LinkCountAsync(count.Id, adjustment.Id, cancellationToken);
            var movements = adjustment.Books
                .Select(book => InventoryMovement.CreateForAdjustment(adjustment.Id, book, now))
                .ToArray();
            await _movements.AddRangeAsync(movements, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        var resultLines = finalization.Lines
            .Select(line => new FinalizedLineResult(line.BookId, line.ReferenceQuantity, line.CountedQuantity, line.Difference))
            .ToArray();

        return new FinalizeCountResult(count.Id, adjustment?.Id, resultLines);
    }

    public async Task<IReadOnlyList<InventoryCountSummary>> ListAsync(CancellationToken cancellationToken = default)
        => await _counts.ListAsync(cancellationToken);

    public async Task<InventoryCountDetail?> GetDetailsAsync(string countId, CancellationToken cancellationToken = default)
    {
        var count = await _counts.FindAsync(countId, cancellationToken);
        if (count is null)
        {
            return null;
        }

        var bookIds = count.Observations
            .Select(observation => observation.BookId)
            .Concat(count.Selections.Select(selection => selection.BookId))
            .Distinct()
            .ToArray();

        var titles = await _books.GetTitlesAsync(bookIds, cancellationToken);
        var quantities = await _stock.GetQuantitiesAsync(bookIds, cancellationToken);
        var employeeNames = await _employees.GetNamesAsync(new[] { count.RecordedByEmployeeId }, cancellationToken);
        var adjustmentId = count.IsFinalized
            ? await _counts.FindAdjustmentIdAsync(count.Id, cancellationToken)
            : null;

        var lines = bookIds
            .Select(bookId =>
            {
                var observations = count.Observations
                    .Where(observation => observation.BookId == bookId)
                    .OrderBy(observation => observation.ObservedAtUtc)
                    .Select(observation => new ObservationDto(
                        observation.Id,
                        observation.CountedQuantity,
                        observation.ObservedAtUtc))
                    .ToArray();

                var selection = count.Selections.FirstOrDefault(item => item.BookId == bookId);
                decimal? countedQuantity = null;
                decimal? difference = null;

                if (selection is not null)
                {
                    countedQuantity = observations
                        .FirstOrDefault(observation => observation.Id == selection.SelectedObservationId)
                        ?.CountedQuantity;
                    difference = countedQuantity - selection.RecordedQuantity;
                }

                return new CountLineDetail(
                    bookId,
                    TitleOf(titles, bookId),
                    quantities.TryGetValue(bookId, out var quantity) ? quantity : 0,
                    observations,
                    selection?.SelectedObservationId,
                    selection?.RecordedQuantity,
                    countedQuantity,
                    difference);
            })
            .ToArray();

        var employeeName = employeeNames.TryGetValue(count.RecordedByEmployeeId, out var name)
            ? name
            : count.RecordedByEmployeeId;

        return new InventoryCountDetail(
            count.Id,
            count.CountedAtUtc,
            count.RecordedByEmployeeId,
            employeeName,
            count.IsFinalized,
            adjustmentId,
            lines);
    }

    private string RequireEmployeeId()
        => _currentUser.EmployeeId
           ?? throw new DomainException("Chưa xác định được tài khoản đang thao tác.");

    private async Task EnsureBooksExistAsync(IEnumerable<string> bookIds, CancellationToken cancellationToken)
    {
        var ids = bookIds.Distinct().ToArray();
        if (!await _books.AllExistAsync(ids, cancellationToken))
        {
            throw new DomainException("Có sách không tồn tại trong danh mục.");
        }
    }

    private static void ValidateLines(IReadOnlyList<NewCountLine> lines)
    {
        if (lines.Count == 0)
        {
            throw new DomainException("Phiếu kiểm kê phải có ít nhất một sách.");
        }

        if (lines.Select(line => line.BookId).Distinct().Count() != lines.Count)
        {
            throw new DomainException("Mỗi sách chỉ được xuất hiện một lần khi tạo phiếu kiểm kê.");
        }

        if (lines.Any(line => string.IsNullOrWhiteSpace(line.BookId)))
        {
            throw new DomainException("Có dòng kiểm kê thiếu sách.");
        }

        if (lines.Any(line => line.CountedQuantity < 0))
        {
            throw new DomainException("Số lượng đếm không được âm.");
        }
    }

    private static string TitleOf(IReadOnlyDictionary<string, string> titles, string bookId)
        => titles.TryGetValue(bookId, out var title) && !string.IsNullOrWhiteSpace(title) ? title : bookId;
}
