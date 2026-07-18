namespace EosToQLab.Core.Exceptions;

public sealed class UnsupportedEosImportFormatException : EosToQLabException
{
    public UnsupportedEosImportFormatException(string fileName)
        : base("EOS_IMPORT_FORMAT_UNSUPPORTED", $"The file '{fileName}' is not a supported EOS CSV or ESF3D source.") { }
}
