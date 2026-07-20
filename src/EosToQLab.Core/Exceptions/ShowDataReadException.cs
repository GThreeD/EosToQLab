namespace EosToQLab.Core.Exceptions;

public sealed class ShowDataReadException : EosToQLabException
{
    public ShowDataReadException(string fileName, Exception innerException)
        : base("EOS_SHOW_ARCHIVE_SHOWDAT_READ_FAILED", $"showdat.dat could not be read from '{fileName}'.", innerException)
    {
    }
}