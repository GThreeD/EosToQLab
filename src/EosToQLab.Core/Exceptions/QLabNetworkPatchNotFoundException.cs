namespace EosToQLab.Core.Exceptions;

public sealed class QLabNetworkPatchNotFoundException : EosToQLabException
{
    public QLabNetworkPatchNotFoundException(string patchName)
        : base("QLAB_NETWORK_PATCH_NOT_FOUND", $"No QLab network patch named '{patchName}' was found in the selected workspace.") { }
}
