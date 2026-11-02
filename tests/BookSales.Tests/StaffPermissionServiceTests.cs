
using BookSales.Application.Access;

namespace BookSales.Tests;

public sealed class StaffPermissionServiceTests
{
    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task ChangeAsync_RespectsPolicy(
        bool allowed,
        int expectedWrites)
    {
        var policy = new FakePolicy(allowed);
        var store = new FakeStore();
        var service = new StaffPermissionService(policy, store);

        var change = new StaffPermissionChange(
            "ACTOR-001",
            "EMP-002",
            "PERMISSION-001",
            true);

        var result = await service.ChangeAsync(change);

        Assert.Equal(allowed, result);
        Assert.Equal(1, policy.CallCount);
        Assert.Equal(expectedWrites, store.CallCount);

        if (allowed)
        {
            Assert.Equal("EMP-002", store.EmployeeId);
            Assert.Equal("PERMISSION-001", store.PermissionId);
            Assert.True(store.Grant);
        }
    }

    [Fact]
    public async Task ChangeAsync_EmptyActor_DoesNotWrite()
    {
        var policy = new FakePolicy(true);
        var store = new FakeStore();
        var service = new StaffPermissionService(policy, store);

        var result = await service.ChangeAsync(
            new StaffPermissionChange(
                "",
                "EMP-002",
                "PERMISSION-001",
                false));

        Assert.False(result);
        Assert.Equal(0, policy.CallCount);
        Assert.Equal(0, store.CallCount);
    }

    private sealed class FakePolicy(bool allowed)
        : IStaffPermissionChangePolicy
    {
        public int CallCount { get; private set; }

        public Task<bool> IsAllowedAsync(
            StaffPermissionChange change,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(allowed);
        }
    }

    private sealed class FakeStore : IStaffPermissionStore
    {
        public int CallCount { get; private set; }
        public string? EmployeeId { get; private set; }
        public string? PermissionId { get; private set; }
        public bool Grant { get; private set; }

        public Task ApplyAsync(
            string employeeId,
            string permissionId,
            bool grant,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            EmployeeId = employeeId;
            PermissionId = permissionId;
            Grant = grant;

            return Task.CompletedTask;
        }
    }
}
