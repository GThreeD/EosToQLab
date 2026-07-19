namespace EosToQLab.Core.Exceptions;

public sealed class QLabWorkspaceNotFoundException : EosToQLabException
{
    public QLabWorkspaceNotFoundException(string workspaceId)
        : base("QLAB_WORKSPACE_NOT_FOUND", $"The open QLab workspace '{workspaceId}' was not found.")
    {
    }
}