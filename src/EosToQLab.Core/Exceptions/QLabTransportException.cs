namespace EosToQLab.Core.Exceptions;

public sealed class QLabTransportException : EosToQLabException
{
    public QLabTransportException(string address, Exception innerException)
        : base(
            "QLAB_TRANSPORT_FAILED",
            $"The QLab TCP connection failed while sending '{address}'.",
            innerException)
    {
    }
}