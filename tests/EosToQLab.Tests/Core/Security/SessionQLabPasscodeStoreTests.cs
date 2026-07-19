using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Security;

public sealed class SessionQLabPasscodeStoreTests
{
    [Fact]
    public async Task Stores_by_normalized_workspace_and_removes_blank_values()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero));
        var store = new SessionQLabPasscodeStore(clock);

        Assert.Null(await store.GetAsync("workspace"));
        await store.SetAsync(" workspace ", "secret");
        Assert.Equal("secret", await store.GetAsync("WORKSPACE"));
        await store.SetAsync("workspace", " ");
        Assert.Null(await store.GetAsync("workspace"));
    }

    [Fact]
    public async Task Expires_values_after_24_hours()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var store = new SessionQLabPasscodeStore(clock);
        await store.SetAsync("workspace", "secret");

        clock.Advance(TimeSpan.FromHours(24));

        Assert.Null(await store.GetAsync("workspace"));
        Assert.Null(await store.GetAsync("workspace"));
    }

    [Fact]
    public async Task Rejects_null_workspace_id()
    {
        var store = new SessionQLabPasscodeStore(
            new FakeTimeProvider(DateTimeOffset.UnixEpoch));

        await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetAsync(null!));

        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetAsync(null!, "secret"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Rejects_invalid_workspace_ids(string workspaceId)
    {
        var store = new SessionQLabPasscodeStore(new FakeTimeProvider(DateTimeOffset.UnixEpoch));
        await Assert.ThrowsAsync<ArgumentException>(() => store.GetAsync(workspaceId));
        await Assert.ThrowsAsync<ArgumentException>(() => store.SetAsync(workspaceId, "secret"));
    }

    [Fact]
    public void Default_constructor_is_available()
    {
        Assert.NotNull(new SessionQLabPasscodeStore());
    }
}