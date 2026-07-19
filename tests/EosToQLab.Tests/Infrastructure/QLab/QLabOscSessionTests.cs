using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Infrastructure.QLab;

public sealed class QLabOscSessionTests
{
    private static readonly QLabWorkspace Workspace = new("workspace-id", "Workspace", null);

    [Fact]
    public void Exposes_workspace()
    {
        var transport = new FakeQLabOscTransport();
        var session = new QLabOscSession(transport, Workspace);

        Assert.Same(Workspace, session.Workspace);
    }

    [Fact]
    public async Task EnableAlwaysReply_sends_expected_command()
    {
        var transport = TransportWithOkReply("/reply/alwaysReply");
        var session = new QLabOscSession(transport, Workspace);

        await session.EnableAlwaysReplyAsync(CancellationToken.None);

        var message = Assert.Single(transport.Messages);
        Assert.Equal("/alwaysReply", message.Address);
        Assert.Equal(1, Assert.Single(message.Arguments));
    }

    [Fact]
    public async Task Reads_cue_lists_network_patches_and_current_cue_list()
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply("/reply/workspace/workspace-id/cueLists/shallow",
            "{\"status\":\"ok\",\"data\":[{\"uniqueID\":\"list-id\",\"name\":\"Main\",\"number\":\"1\"}]}");
        transport.EnqueueReply("/reply/workspace/workspace-id/settings/network/patchList",
            "{\"status\":\"ok\",\"data\":[{\"uniqueID\":\"patch-id\",\"name\":\"EOS\",\"type\":\"eos\"}]}");
        transport.EnqueueReply("/reply/workspace/workspace-id/currentCueListID",
            "{\"status\":\"ok\",\"data\":\"list-id\"}");
        var session = new QLabOscSession(transport, Workspace);

        var cueList = Assert.Single(await session.GetCueListsAsync(TestContext.Current.CancellationToken));
        var patch = Assert.Single(await session.GetNetworkPatchesAsync(TestContext.Current.CancellationToken));
        var currentCueListId = await session.GetCurrentCueListIdAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new QLabCueList("list-id", "Main", "1"), cueList);
        Assert.Equal(new QLabNetworkPatch("patch-id", "EOS", "eos"), patch);
        Assert.Equal("list-id", currentCueListId);
    }

    [Theory]
    [InlineData(QLabCueType.CueList, "cue list")]
    [InlineData(QLabCueType.Memo, "memo")]
    [InlineData(QLabCueType.Network, "network")]
    public async Task CreateCue_sends_type_and_returns_id(QLabCueType cueType, string protocolType)
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply("/reply/workspace/workspace-id/new",
            "{\"status\":\"ok\",\"data\":\"new-cue-id\"}");
        var session = new QLabOscSession(transport, Workspace);

        var cueId = await session.CreateCueAsync(cueType, "Cue", TestContext.Current.CancellationToken);

        Assert.Equal("new-cue-id", cueId);
        var message = Assert.Single(transport.Messages);
        Assert.Equal("/workspace/workspace-id/new", message.Address);
        Assert.Equal(protocolType, Assert.Single(message.Arguments));
    }

    [Fact]
    public async Task CreateCue_rejects_missing_id_without_wrapping_domain_exception()
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply("/reply/workspace/workspace-id/new", "{\"status\":\"ok\",\"data\":null}");
        var session = new QLabOscSession(transport, Workspace);

        await Assert.ThrowsAsync<QLabUnexpectedReplyException>(() =>
            session.CreateCueAsync(QLabCueType.Network, "Cue", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(QLabCueType.CueList, typeof(QLabCueListCreationException))]
    [InlineData(QLabCueType.Memo, typeof(QLabCueCreationException))]
    [InlineData(QLabCueType.Network, typeof(QLabCueCreationException))]
    public async Task CreateCue_wraps_non_domain_failures(QLabCueType cueType, Type expectedExceptionType)
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply(_ => throw new InvalidOperationException("boom"));
        var session = new QLabOscSession(transport, Workspace);

        var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
            session.CreateCueAsync(cueType, "Cue", TestContext.Current.CancellationToken));

        Assert.Equal(expectedExceptionType, exception.GetType());
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task CreateCue_does_not_wrap_cancellation()
    {
        var transport = new FakeQLabOscTransport();
        var session = new QLabOscSession(transport, Workspace);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            session.CreateCueAsync(QLabCueType.Network, "Cue", cancellation.Token));
    }

    [Fact]
    public async Task Sets_and_queries_cue_workspace_and_network_properties()
    {
        var transport = new FakeQLabOscTransport();
        for (var index = 0; index < 3; index++)
            transport.EnqueueReply("/reply/ok", "{\"status\":\"ok\"}");
        transport.EnqueueReply("/reply/query", "{\"status\":\"ok\",\"data\":\"Cue name\"}");
        var session = new QLabOscSession(transport, Workspace);

        await session.SetCuePropertyAsync("cue-id", QLabCueProperty.Notes, "Notes",
            TestContext.Current.CancellationToken);
        await session.SetWorkspacePropertyAsync(QLabWorkspaceProperty.CurrentCueListId, "list-id",
            TestContext.Current.CancellationToken);
        await session.SetNetworkParameterAsync("cue-id", 4, "83.1", TestContext.Current.CancellationToken);
        var value = await session.QueryCuePropertyAsync("cue-id", QLabCueProperty.Name,
            TestContext.Current.CancellationToken);

        Assert.Equal("Cue name", value);
        Assert.Equal("/workspace/workspace-id/cue_id/cue-id/notes", transport.Messages[0].Address);
        Assert.Equal("/workspace/workspace-id/currentCueListID", transport.Messages[1].Address);
        Assert.Equal("/workspace/workspace-id/cue_id/cue-id/parameterValue/4", transport.Messages[2].Address);
        Assert.Equal("/workspace/workspace-id/cue_id/cue-id/name", transport.Messages[3].Address);
    }

    [Fact]
    public async Task RenameCueList_sets_and_verifies_name()
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply("/reply/set", "{\"status\":\"ok\"}");
        transport.EnqueueReply("/reply/query", "{\"status\":\"ok\",\"data\":\"New\"}");
        var session = new QLabOscSession(transport, Workspace);

        await session.RenameCueListAsync("list-id", "Old", "New", TestContext.Current.CancellationToken);

        Assert.Equal(2, transport.Messages.Count);
    }

    [Fact]
    public async Task RenameCueList_reports_verification_failure_directly()
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply("/reply/set", "{\"status\":\"ok\"}");
        transport.EnqueueReply("/reply/query", "{\"status\":\"ok\",\"data\":\"Wrong\"}");
        var session = new QLabOscSession(transport, Workspace);

        await Assert.ThrowsAsync<QLabCueListRenameVerificationException>(() =>
            session.RenameCueListAsync("list-id", "Old", "New", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RenameCueList_wraps_transport_failure_but_not_cancellation()
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply(_ => throw new InvalidOperationException("boom"));
        var session = new QLabOscSession(transport, Workspace);

        var wrapped = await Assert.ThrowsAsync<QLabCueListRenameException>(() =>
            session.RenameCueListAsync("list-id", "Old", "New", TestContext.Current.CancellationToken));
        Assert.IsType<InvalidOperationException>(wrapped.InnerException);

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            session.RenameCueListAsync("list-id", "Old", "New", cancellation.Token));
    }

    [Fact]
    public async Task DeleteCueList_deletes_and_verifies_absence()
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply("/reply/delete", "{\"status\":\"ok\"}");
        transport.EnqueueReply("/reply/lists", "{\"status\":\"ok\",\"data\":[]}");
        var session = new QLabOscSession(transport, Workspace);

        await session.DeleteCueListAsync("list-id", "List", TestContext.Current.CancellationToken);

        Assert.Equal("/workspace/workspace-id/delete_id/list-id", transport.Messages[0].Address);
    }

    [Fact]
    public async Task DeleteCueList_reports_verification_failure_directly()
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply("/reply/delete", "{\"status\":\"ok\"}");
        transport.EnqueueReply("/reply/lists",
            "{\"status\":\"ok\",\"data\":[{\"uniqueID\":\"LIST-ID\",\"name\":\"List\"}]}");
        var session = new QLabOscSession(transport, Workspace);

        await Assert.ThrowsAsync<QLabCueListDeletionVerificationException>(() =>
            session.DeleteCueListAsync("list-id", "List", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteCueList_wraps_transport_failure_but_not_cancellation()
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply(_ => throw new InvalidOperationException("boom"));
        var session = new QLabOscSession(transport, Workspace);

        var wrapped = await Assert.ThrowsAsync<QLabCueListDeletionException>(() =>
            session.DeleteCueListAsync("list-id", "List", TestContext.Current.CancellationToken));
        Assert.IsType<InvalidOperationException>(wrapped.InnerException);

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            session.DeleteCueListAsync("list-id", "List", cancellation.Token));
    }

    [Fact]
    public async Task Save_wraps_failures_and_undo_sends_command()
    {
        var saveTransport = new FakeQLabOscTransport();
        saveTransport.EnqueueReply(_ => throw new InvalidOperationException("disk"));
        var saveSession = new QLabOscSession(saveTransport, Workspace);

        var exception = await Assert.ThrowsAsync<QLabWorkspaceSaveException>(() =>
            saveSession.SaveWorkspaceAsync(TestContext.Current.CancellationToken));
        Assert.IsType<InvalidOperationException>(exception.InnerException);

        var undoTransport = TransportWithOkReply("/reply/undo");
        var undoSession = new QLabOscSession(undoTransport, Workspace);
        await undoSession.UndoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("/workspace/workspace-id/undo", Assert.Single(undoTransport.Messages).Address);
    }

    [Fact]
    public async Task Save_does_not_wrap_cancellation()
    {
        var transport = new FakeQLabOscTransport();
        var session = new QLabOscSession(transport, Workspace);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.SaveWorkspaceAsync(cancellation.Token));
    }

    [Fact]
    public async Task Dispose_disposes_transport()
    {
        var transport = new FakeQLabOscTransport();
        var session = new QLabOscSession(transport, Workspace);

        await session.DisposeAsync();

        Assert.Equal(1, transport.DisposeCount);
    }

    [Fact]
    public async Task Failed_reply_is_disposed_and_propagated()
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply("/reply/test", "{\"status\":\"error\"}");
        var session = new QLabOscSession(transport, Workspace);

        await Assert.ThrowsAsync<QLabUnexpectedReplyException>(() =>
            session.SetCuePropertyAsync("cue-id", QLabCueProperty.Name, "Name", TestContext.Current.CancellationToken));
    }

    private static FakeQLabOscTransport TransportWithOkReply(string replyAddress)
    {
        var transport = new FakeQLabOscTransport();
        transport.EnqueueReply(replyAddress, "{\"status\":\"ok\"}");
        return transport;
    }
}