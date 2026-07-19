namespace EosToQLab.Tests.Core.Models;

public sealed class QLabNetworkCuePlanTests
{
    [Fact]
    public void Stores_all_network_cue_values()
    {
        var item = new QLabNetworkCuePlan("Label", "2", "83.1", "83.1", "Notes", false);

        Assert.Equal("Label", item.Name);
        Assert.Equal("2", item.ListNumber);
        Assert.Equal("83.1", item.CueNumber);
        Assert.Equal("83.1", item.QLabNumber);
        Assert.Equal("Notes", item.Notes);
        Assert.False(item.Armed);
        Assert.IsAssignableFrom<QLabPlanItem>(item);
    }
}