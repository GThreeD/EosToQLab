namespace EosToQLab.Tests.Core.Models;

public sealed class QLabPlanItemTests
{
    [Fact]
    public void Stores_constructor_values()
    {
        QLabPlanItem network = new QLabNetworkCuePlan("Name", "1", "2", "2", "Notes", false);
        QLabPlanItem memo = new QLabMemoCuePlan("Scene", null);
        Assert.IsType<QLabNetworkCuePlan>(network);
        Assert.IsType<QLabMemoCuePlan>(memo);
    }
}