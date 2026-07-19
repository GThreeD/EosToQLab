namespace EosToQLab.Tests.Infrastructure.QLab.Osc;

public sealed class IQLabOscTransportTests
{
    [Fact]
    public void Defines_async_transport_contract()
    {
        Assert.True(typeof(IQLabOscTransport).GetInterfaces().Contains(typeof(IAsyncDisposable)));
    }
}