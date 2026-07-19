namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabNetworkParameterAssignmentTests
{
    [Fact]
    public void Stores_index_parameter_and_value()
    {
        var assignment = new QLabNetworkParameterAssignment(3, QLabEosParameter.List, "2");

        Assert.Equal(3, assignment.Index);
        Assert.Equal(QLabEosParameter.List, assignment.Parameter);
        Assert.Equal("2", assignment.Value);
    }
}