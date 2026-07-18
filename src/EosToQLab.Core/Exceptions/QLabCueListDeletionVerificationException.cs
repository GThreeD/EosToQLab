namespace EosToQLab.Core.Exceptions;

public sealed class QLabCueListDeletionVerificationException : EosToQLabException
{
    public QLabCueListDeletionVerificationException(string cueListName)
        : base(
            "QLAB_CUE_LIST_DELETION_NOT_APPLIED",
            $"QLab still reports cue list '{cueListName}' after the deletion request.") { }
}
