namespace EosToQLab.Core.Exceptions;

public sealed class QLabCueListDeletionException : EosToQLabException
{
    public QLabCueListDeletionException(string cueListName, Exception innerException)
        : base(
            "QLAB_CUE_LIST_DELETION_FAILED",
            $"The QLab cue list '{cueListName}' could not be deleted.",
            innerException) { }
}
