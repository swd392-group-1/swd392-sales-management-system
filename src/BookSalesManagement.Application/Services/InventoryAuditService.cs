using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Common;
using BookSalesManagement.Application.Dtos;
using BookSalesManagement.Domain.Common;
using BookSalesManagement.Domain.Inventory;

namespace BookSalesManagement.Application.Services;

public sealed class InventoryAuditService
{
    private readonly IInventoryAuditStore _audits;
    private readonly IStockStore _stock;
    private readonly IBookDirectory _books;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public InventoryAuditService(
        IInventoryAuditStore audits,
        IStockStore stock,
        IBookDirectory books,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _audits = audits;
        _stock = stock;
        _books = books;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<string> CreateAsync(IReadOnlyList<NewCountLine> lines, CancellationToken cancellationToken = default)
    {
        _ = _currentUser.EmployeeId
            ?? throw new DomainException("Chưa xác định được tài khoản đang thao tác.");

        if (lines.Count == 0)
        {
            throw new DomainException("Đợt audit phải có ít nhất một sách.");
        }

        if (lines.Select(line => line.BookId).Distinct().Count() != lines.Count)
        {
            throw new DomainException("Mỗi sách chỉ được xuất hiện một lần trong một đợt audit.");
        }

        var bookIds = lines.Select(line => line.BookId).ToArray();
        if (!await _books.AllExistAsync(bookIds, cancellationToken))
        {
            throw new DomainException("Có sách không tồn tại trong danh mục.");
        }

        var now = _clock.UtcNow;
        var audit = new InventoryAudit(IdFactory.New("AUD", now), DateOnly.FromDateTime(now));
        foreach (var line in lines)
        {
            audit.AddLine(line.BookId, line.CountedQuantity);
        }

        await _audits.AddAsync(audit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return audit.Id;
    }

    public async Task ApproveAsync(string auditId, CancellationToken cancellationToken = default)
    {
        var audit = await _audits.FindAsync(auditId, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy đợt audit {auditId}.");

        audit.Approve();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryAuditSummary>> ListAsync(CancellationToken cancellationToken = default)
        => await _audits.ListAsync(cancellationToken);

    public async Task<InventoryAuditDetail?> GetDetailsAsync(string auditId, CancellationToken cancellationToken = default)
    {
        var audit = await _audits.FindAsync(auditId, cancellationToken);
        if (audit is null)
        {
            return null;
        }

        var bookIds = audit.Lines.Select(line => line.BookId).Distinct().ToArray();
        var titles = await _books.GetTitlesAsync(bookIds, cancellationToken);
        var quantities = await _stock.GetQuantitiesAsync(bookIds, cancellationToken);

        var lines = audit.Lines
            .Select(line => new AuditLineDetail(
                line.Id,
                line.BookId,
                TitleOf(titles, line.BookId),
                line.CountedQuantity,
                quantities.TryGetValue(line.BookId, out var quantity) ? quantity : 0))
            .ToArray();

        return new InventoryAuditDetail(
            audit.Id,
            audit.AuditDate,
            audit.Status ?? InventoryAudit.DraftStatus,
            audit.IsApproved,
            lines);
    }

    private static string TitleOf(IReadOnlyDictionary<string, string> titles, string bookId)
        => titles.TryGetValue(bookId, out var title) && !string.IsNullOrWhiteSpace(title) ? title : bookId;
}
