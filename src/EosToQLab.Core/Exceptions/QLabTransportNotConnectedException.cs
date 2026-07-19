namespace EosToQLab.Core.Exceptions;

public sealed class QLabTransportNotConnectedException : EosToQLabException
{
    public QLabTransportNotConnectedException()
        : base("QLAB_TRANSPORT_NOT_CONNECTED", "The QLab OSC transport is not connected.")
    {
    }
}