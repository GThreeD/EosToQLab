namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabPlanExecutionContextTests
{
    [Fact]
    public void Stores_network_patch()
    {
        var patch = new QLabNetworkPatch("id", "EOS", "eos");

        Assert.Equal(patch, new QLabPlanExecutionContext(patch).NetworkPatch);
    }
}