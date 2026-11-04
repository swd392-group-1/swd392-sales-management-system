using System.Text;
using BookSales.Application.Reports;
using BookSales.Application.Services;
using BookSales.Domain.Entities;
using BookSales.Domain.Interfaces;

namespace BookSales.Tests;
public sealed class LeadIntegrationTests
{
    [Fact]
    public async Task ReportPreservesUnknownStockAndNormalizesFilter()
    {
        var query = new Query();
        var report = await new InventoryReportService(query, TimeProvider.System)
            .GenerateAsync(new("  abc  ", true), default);
        Assert.Equal("abc", query.Filter?.Search);
        Assert.True(query.Filter?.InStockOnly);
        Assert.Equal(2, report.Rows.Count);
        Assert.Equal(3m, report.TotalQuantity);
        Assert.Equal(1, report.MissingStockCount);
    }
    [Fact]
    public void CsvEscapesQuotesNewlinesAndSpreadsheetFormulas()
    {
        var report = new InventoryReport(DateTimeOffset.UtcNow,
            new[] { new InventoryReportRow("=SUM(1)", "A,\"B\"\nC", 12.5m, null) });
        var csv = Encoding.UTF8.GetString(InventoryCsv.Export(report));
        Assert.Contains("\"'=SUM(1)\"", csv);
        Assert.Contains("\"A,\"\"B\"\"\nC\"", csv);
        Assert.Contains(",12.5,\r\n", csv);
    }
    [Fact]
    public async Task PricingQueuesOnlyAfterSuccessfulCommit()
    {
        var repository = new Repository(); var queue = new Queue(repository);
        Assert.True(await new PricingService(repository, queue).UpdateCurrentPriceAsync("B1", 99m));
        Assert.True(queue.Queued);
        repository.Fail = true; queue.Queued = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new PricingService(repository, queue).UpdateCurrentPriceAsync("B1", 101m));
        Assert.False(queue.Queued);
    }
    private sealed class Query : IInventoryReportQuery
    {
        public InventoryReportFilter? Filter { get; private set; }
        public Task<IReadOnlyList<InventoryReportRow>> ReadAsync(InventoryReportFilter filter, CancellationToken ct)
        {
            Filter = filter;
            return Task.FromResult<IReadOnlyList<InventoryReportRow>>(new[] {
                new InventoryReportRow("B1", "Known", 10m, 3m), new InventoryReportRow("B2", null, null, null) });
        }
    }
    private sealed class Repository : IBookRepository
    {
        public bool Committed { get; private set; }
        public bool Fail { get; set; }
        public Task<Book?> GetByIdAsync(string id) => Task.FromResult<Book?>(new Book { BookId = id });
        public Task SaveAndCommitAsync()
        {
            if (Fail) throw new InvalidOperationException("Simulated commit failure");
            Committed = true; return Task.CompletedTask;
        }
    }
    private sealed class Queue(Repository repository) : IBackgroundTaskQueue
    {
        public bool Queued { get; set; }
        public ValueTask EnqueueAsync(object item)
        {
            Assert.True(repository.Committed); Queued = true; return ValueTask.CompletedTask;
        }
    }
}
