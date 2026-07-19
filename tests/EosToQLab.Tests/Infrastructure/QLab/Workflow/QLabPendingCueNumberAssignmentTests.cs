namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabPendingCueNumberAssignmentTests
{
    [Fact]
    public void Stores_number_assignment_values()
    {
        var assignment = new QLabPendingCueNumberAssignment("cue-id", "Cue", "83.1");

        Assert.Equal("cue-id", assignment.CueId);
        Assert.Equal("Cue", assignment.CueName);
        Assert.Equal("83.1", assignment.DesiredCueNumber);
    }
}