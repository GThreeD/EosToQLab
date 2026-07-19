namespace EosToQLab.Tests.Infrastructure.QLab.Osc;

public sealed class QLabTcpOscTransportFactoryTests
{
    [Fact]
    public void Creates_tcp_transport()
    {
        Assert.IsType<QLabTcpOscTransport>(new QLabTcpOscTransportFactory().Create());
    }
}