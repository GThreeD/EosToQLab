namespace EosToQLab.Core.Exceptions;

public sealed class QLabRequestTimeoutException : EosToQLabException
{
    public QLabRequestTimeoutException(string address, TimeSpan timeout, Exception innerException)
        : base(
            "QLAB_REQUEST_TIMEOUT",
            $"QLab did not reply to '{address}' within {timeout.TotalSeconds:0} seconds.",
            innerException)
    {
    }
}