namespace EosToQLab.Core.Exceptions;

public sealed class QLabCueListConflictException : EosToQLabException
{
    public QLabCueListConflictException(string cueListName)
        : base("QLAB_CUE_LIST_CONFLICT",
            $"A cue list named '{cueListName}' already exists. Explicit replacement consent is required.")
    {
        CueListName = cueListName;
    }

    public string CueListName { get; }
}