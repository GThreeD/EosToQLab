namespace EosToQLab.Core.Exceptions;

public sealed class QLabNoOpenWorkspaceException : EosToQLabException
{
    public QLabNoOpenWorkspaceException()
        : base("QLAB_NO_OPEN_WORKSPACE", "QLab is reachable, but no workspace is currently open.") { }
}
