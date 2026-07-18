namespace EosToQLab.Core.Exceptions;

public sealed class QLabCueCreationException : EosToQLabException
{
    public QLabCueCreationException(string cueName, Exception innerException)
        : base("QLAB_CUE_CREATION_FAILED", $"The QLab cue '{cueName}' could not be created.", innerException) { }
}
