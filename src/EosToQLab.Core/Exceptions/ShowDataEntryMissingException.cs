namespace EosToQLab.Core.Exceptions;

public sealed class ShowDataEntryMissingException : EosToQLabException
{
    public ShowDataEntryMissingException(string fileName)
        : base("EOS_SHOW_ARCHIVE_SHOWDAT_MISSING", $"The EOS show archive '{fileName}' does not contain showdat.dat.")
    {
    }
}