namespace EosToQLab.Core.Exceptions;

public sealed class QLabOscPacketException : EosToQLabException
{
    public QLabOscPacketException(string message, Exception? innerException = null)
        : base("QLAB_OSC_PACKET_INVALID", message, innerException) { }
}
