namespace EosToQLab.Core.Exceptions;

public sealed class EosShowArchiveInvalidException : EosToQLabException
{
    public EosShowArchiveInvalidException(string fileName, Exception innerException)
        : base("EOS_SHOW_ARCHIVE_INVALID", $"The EOS show archive '{fileName}' is not a valid ZIP archive.",
            innerException)
    {
    }
}