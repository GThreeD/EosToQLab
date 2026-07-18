namespace EosToQLab.Core.Exceptions;

public sealed class QLabConnectionClosedException : EosToQLabException
{
    public QLabConnectionClosedException()
        : base(
            "QLAB_CONNECTION_CLOSED",
            "The QLab TCP connection closed while an OSC frame was being read.") { }
}
