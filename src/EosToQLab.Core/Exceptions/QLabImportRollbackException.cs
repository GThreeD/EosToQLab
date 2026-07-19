namespace EosToQLab.Core.Exceptions;

public sealed class QLabImportRollbackException : EosToQLabException
{
    public QLabImportRollbackException(string temporaryCueListId, Exception innerException)
        : base("QLAB_IMPORT_ROLLBACK_FAILED",
            $"The temporary QLab cue list '{temporaryCueListId}' could not be removed after the import failed.",
            innerException)
    {
    }
}