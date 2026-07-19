using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Infrastructure.QLab;

public sealed class QLabServiceTests
{
    [Fact]
    public async Task Forwards_workspace_cue_list_patch_and_import_operations()
    {
        var session = new FakeQLabOscSession
        {
            CueLists = [new QLabCueList("list", "List", null)],
            NetworkPatches = [new QLabNetworkPatch("patch-id", "Eos", "eos")]
        };
        var osc = new FakeQLabOscService(session);
        var workflow = new QLabImportWorkflow(osc, new QLabImportPlanBuilder(),
            new QLabImportPlanExecutor([new QLabMemoCuePlanMapper(), new QLabNetworkCuePlanMapper()]));
        var service = new QLabService(osc, workflow);

        Assert.Equal(osc.Workspaces, await service.GetOpenWorkspacesAsync(TestContext.Current.CancellationToken));
        Assert.Equal(session.CueLists,
            await service.GetCueListsAsync("workspace", "pass", TestContext.Current.CancellationToken));
        Assert.Equal(session.NetworkPatches,
            await service.GetNetworkPatchesAsync("workspace", "pass", TestContext.Current.CancellationToken));
        var result =
            await service.ImportAsync([TestData.Cue()], TestData.Options(), TestContext.Current.CancellationToken);
        Assert.Equal(1, result.NetworkCueCount);
        Assert.Equal(3, osc.Connections.Count);
        Assert.Equal(3, session.DisposeCount);
    }
}