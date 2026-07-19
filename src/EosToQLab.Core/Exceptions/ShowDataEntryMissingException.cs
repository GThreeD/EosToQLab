namespace EosToQLab.Core.Exceptions;

public sealed class ShowDataEntryMissingException : EosToQLabException
{
    public ShowDataEntryMissingException(string fileName)
        : base("EOS_ESF3D_SHOWDAT_MISSING", $"The ESF3D archive '{fileName}' does not contain showdat.dat.")
    {
    }
}