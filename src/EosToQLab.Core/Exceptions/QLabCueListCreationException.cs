namespace EosToQLab.Core.Exceptions;

public sealed class QLabCueListCreationException : EosToQLabException
{
    public QLabCueListCreationException(string cueListName, Exception innerException)
        : base("QLAB_CUE_LIST_CREATION_FAILED", $"The QLab cue list '{cueListName}' could not be created.", innerException) { }
}
