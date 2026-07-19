namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabCuePropertyAssignmentTests
{
    [Fact]
    public void Stores_property_and_value()
    {
        var assignment = new QLabCuePropertyAssignment(QLabCueProperty.Notes, "Notes");

        Assert.Equal(QLabCueProperty.Notes, assignment.Property);
        Assert.Equal("Notes", assignment.Value);
    }
}