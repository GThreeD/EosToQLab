namespace EosToQLab.Tests.Infrastructure.QLab.Osc;

public sealed class IQLabOscTransportFactoryTests
{
    [Fact]
    public void Defines_transport_factory_contract()
    {
        Assert.True(typeof(IQLabOscTransportFactory).IsInterface);
    }
}