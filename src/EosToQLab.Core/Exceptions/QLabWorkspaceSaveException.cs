namespace EosToQLab.Core.Exceptions;

public sealed class QLabWorkspaceSaveException : EosToQLabException
{
    public QLabWorkspaceSaveException(string workspaceName, Exception innerException)
        : base("QLAB_WORKSPACE_SAVE_FAILED", $"The QLab workspace '{workspaceName}' could not be saved.", innerException) { }
}
