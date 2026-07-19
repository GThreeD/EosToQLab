namespace EosToQLab.Core.Exceptions;

public sealed class QLabUnexpectedReplyException : EosToQLabException
{
    public QLabUnexpectedReplyException(string address, string reply)
        : base("QLAB_UNEXPECTED_REPLY", $"QLab returned an unexpected reply for '{address}': {reply}")
    {
    }
}