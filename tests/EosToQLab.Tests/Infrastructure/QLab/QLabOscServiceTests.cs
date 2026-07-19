using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Infrastructure.QLab;

public sealed class QLabOscServiceTests
{
    [Fact]
    public async Task GetOpenWorkspaces_connects_parses_and_disposes_transport()
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply("/reply/workspaces",
            "{\"status\":\"ok\",\"data\":[{\"uniqueID\":\"w\",\"displayName\":\"Workspace\"}]}");
        var service = new QLabOscService(new FakeQLabOscTransportFactory(() => transport));

        var workspace = Assert.Single(await service.GetOpenWorkspacesAsync(TestContext.Current.CancellationToken));

        Assert.Equal("w", workspace.Id);
        Assert.Equal(1, transport.ConnectCount);
        Assert.Equal(1, transport.DisposeCount);
        Assert.Equal("/workspaces", Assert.Single(transport.Messages).Address);
    }

    [Fact]
    public async Task GetOpenWorkspaces_rejects_empty_list()
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply("/reply/workspaces", "{\"status\":\"ok\",\"data\":[]}");
        await Assert.ThrowsAsync<QLabNoOpenWorkspaceException>(() =>
            new QLabOscService(new FakeQLabOscTransportFactory(() => transport)).GetOpenWorkspacesAsync(TestContext
                .Current.CancellationToken));
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("pass", 1)]
    public async Task ConnectWorkspace_finds_workspace_connects_with_optional_passcode_and_enables_replies(
        string? passcode, int connectArgumentCount)
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply("/reply/workspaces",
            "{\"status\":\"ok\",\"data\":[{\"id\":\"w\",\"name\":\"Workspace\"}]}");
        transport.EnqueueReply("/reply/workspace/w/connect", "{\"status\":\"ok\"}");
        transport.EnqueueReply("/reply/alwaysReply", "{\"status\":\"ok\"}");
        var service = new QLabOscService(new FakeQLabOscTransportFactory(() => transport));

        await using var session =
            await service.ConnectWorkspaceAsync("W", passcode, TestContext.Current.CancellationToken);

        Assert.Equal("w", session.Workspace.Id);
        Assert.Equal(3, transport.Messages.Count);
        Assert.Equal(connectArgumentCount, transport.Messages[1].Arguments.Count);
        Assert.Equal("/alwaysReply", transport.Messages[2].Address);
        Assert.Equal(1, transport.Messages[2].Arguments.Single());
    }

    [Fact]
    public async Task ConnectWorkspace_disposes_transport_on_lookup_or_authentication_failure()
    {
        var missing = new FakeQLabOscTransport();
        missing.EnqueueReply("/reply/workspaces", "{\"status\":\"ok\",\"data\":[]}");
        await Assert.ThrowsAsync<QLabWorkspaceNotFoundException>(() =>
            new QLabOscService(new FakeQLabOscTransportFactory(() => missing)).ConnectWorkspaceAsync("missing", null,
                TestContext.Current.CancellationToken));
        Assert.Equal(1, missing.DisposeCount);

        var denied = new FakeQLabOscTransport();
        denied.EnqueueReply("/reply/workspaces", "{\"status\":\"ok\",\"data\":[{\"id\":\"w\"}]}");
        denied.EnqueueReply("/reply/workspace/w/connect", "{\"status\":\"denied\"}");
        await Assert.ThrowsAsync<QLabAccessDeniedException>(() =>
            new QLabOscService(new FakeQLabOscTransportFactory(() => denied)).ConnectWorkspaceAsync("w", "bad",
                TestContext.Current.CancellationToken));
        Assert.Equal(1, denied.DisposeCount);
    }

    [Fact]
    public void Default_constructor_is_available()
    {
        Assert.NotNull(new QLabOscService());
    }
}