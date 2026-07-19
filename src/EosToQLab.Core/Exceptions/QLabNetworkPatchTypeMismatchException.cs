namespace EosToQLab.Core.Exceptions;

public sealed class QLabNetworkPatchTypeMismatchException : EosToQLabException
{
    public QLabNetworkPatchTypeMismatchException(string patchName, string patchType)
        : base("QLAB_NETWORK_PATCH_TYPE_MISMATCH",
            $"The QLab network patch '{patchName}' has type '{patchType}', not an ETC Eos family type.")
    {
    }
}