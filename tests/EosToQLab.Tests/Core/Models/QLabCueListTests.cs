namespace EosToQLab.Tests.Core.Models;

public sealed class QLabCueListTests
{
    [Fact]
    public void Stores_constructor_values()
    {
        var value = new QLabCueList("id", "name", "1");
        Assert.Equal(("id", "name", "1"), (value.Id, value.Name, value.Number));
    }
}