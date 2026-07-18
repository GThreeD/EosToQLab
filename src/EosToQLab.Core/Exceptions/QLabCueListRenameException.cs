namespace EosToQLab.Core.Exceptions;

public sealed class QLabCueListRenameException : EosToQLabException
{
    public QLabCueListRenameException(string cueListName, string targetName, Exception innerException)
        : base(
            "QLAB_CUE_LIST_RENAME_FAILED",
            $"The QLab cue list '{cueListName}' could not be renamed to '{targetName}'.",
            innerException) { }
}
