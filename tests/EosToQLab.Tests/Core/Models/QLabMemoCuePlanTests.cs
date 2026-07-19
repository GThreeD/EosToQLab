namespace EosToQLab.Tests.Core.Models;

public sealed class QLabMemoCuePlanTests
{
    [Fact]
    public void Uses_safe_defaults_and_stores_values()
    {
        var item = new QLabMemoCuePlan("Scene", null);

        Assert.Equal("Scene", item.Name);
        Assert.Null(item.Notes);
        Assert.False(item.Armed);
        Assert.True(item.SkipIfDisarmed);
        Assert.IsAssignableFrom<QLabPlanItem>(item);
    }
}