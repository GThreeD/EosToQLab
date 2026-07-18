namespace EosToQLab.Core.Exceptions;

public sealed class QLabCueListRenameVerificationException : EosToQLabException
{
    public QLabCueListRenameVerificationException(string expectedName, string? actualName)
        : base(
            "QLAB_CUE_LIST_RENAME_NOT_APPLIED",
            $"QLab did not apply cue list name '{expectedName}'. The reported name is '{actualName ?? "<empty>"}'.") { }
}
