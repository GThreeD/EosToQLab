namespace EosToQLab.Core.Exceptions;

public sealed class QLabConnectionException : EosToQLabException
{
    public QLabConnectionException(string host, int port, Exception innerException)
        : base("QLAB_CONNECTION_FAILED", $"A TCP connection to QLab at {host}:{port} could not be established.", innerException) { }
}
