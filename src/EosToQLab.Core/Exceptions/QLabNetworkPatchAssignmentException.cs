namespace EosToQLab.Core.Exceptions;

public sealed class QLabNetworkPatchAssignmentException : EosToQLabException
{
    public QLabNetworkPatchAssignmentException(string cueName, string patchName)
        : base(
            "QLAB_NETWORK_PATCH_ASSIGNMENT_FAILED",
            $"QLab did not apply network patch '{patchName}' to cue '{cueName}'.") { }
}
