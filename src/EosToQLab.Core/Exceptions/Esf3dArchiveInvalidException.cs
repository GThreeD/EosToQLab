namespace EosToQLab.Core.Exceptions;

public sealed class Esf3dArchiveInvalidException : EosToQLabException
{
    public Esf3dArchiveInvalidException(string fileName, Exception innerException)
        : base("EOS_ESF3D_ARCHIVE_INVALID", $"The ESF3D file '{fileName}' is not a valid ZIP archive.", innerException) { }
}
