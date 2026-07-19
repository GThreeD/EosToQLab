namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabPlanExecutionResultTests
{
    [Fact]
    public void Stores_pending_assignments()
    {
        var pending = new QLabPendingCueNumberAssignment("cue-id", "Cue", "1");
        var result = new QLabPlanExecutionResult([pending]);

        Assert.Equal([pending], result.PendingCueNumbers);
    }
}