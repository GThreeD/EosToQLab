namespace EosToQLab.Tests.Infrastructure.QLab.Osc;

public sealed class OscMessageTests
{
    [Fact]
    public void Params_constructor_stores_address_and_arguments()
    {
        var message = new OscMessage("/test", "value", 1);
        Assert.Equal("/test", message.Address);
        Assert.Equal(["value", 1], message.Arguments);
    }
}