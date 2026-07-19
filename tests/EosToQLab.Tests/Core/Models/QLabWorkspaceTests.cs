namespace EosToQLab.Tests.Core.Models;

public sealed class QLabWorkspaceTests
{
    [Fact]
    public void Stores_constructor_values()
    {
        var value = new QLabWorkspace("id", "Workspace", "/tmp/test");
        Assert.Equal(("id", "Workspace", "/tmp/test"), (value.Id, value.Name, value.Path));
    }
}