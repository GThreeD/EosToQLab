namespace EosToQLab.Infrastructure.QLab.Osc;

internal sealed class QLabTcpOscTransportFactory : IQLabOscTransportFactory
{
    public IQLabOscTransport Create() => new QLabTcpOscTransport();
}
