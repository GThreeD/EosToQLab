namespace EosToQLab.Core.Exceptions;

public sealed class QLabAccessDeniedException : EosToQLabException
{
    public QLabAccessDeniedException(string workspaceName)
        : base("QLAB_ACCESS_DENIED", $"QLab denied edit access to workspace '{workspaceName}'. Check the OSC passcode and permissions.") { }
}
