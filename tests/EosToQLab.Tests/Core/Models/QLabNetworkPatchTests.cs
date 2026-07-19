namespace EosToQLab.Tests.Core.Models;

public sealed class QLabNetworkPatchTests
{
    [Fact]
    public void Stores_constructor_values()
    {
        var value = new QLabNetworkPatch("id", "Eos", "eos");
        Assert.Equal(("id", "Eos", "eos"), (value.Id, value.Name, value.Type));
    }
}