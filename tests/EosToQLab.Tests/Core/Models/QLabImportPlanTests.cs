namespace EosToQLab.Tests.Core.Models;

public sealed class QLabImportPlanTests
{
    [Fact]
    public void Counts_memo_and_network_items()
    {
        var plan = new QLabImportPlan([
            new QLabMemoCuePlan("Scene", null),
            new QLabNetworkCuePlan("Cue", "1", "1", "1", null),
            new QLabNetworkCuePlan("Cue 2", "1", "2", "2", null)
        ]);
        Assert.Equal(2, plan.NetworkCueCount);
        Assert.Equal(1, plan.MemoCueCount);
    }
}