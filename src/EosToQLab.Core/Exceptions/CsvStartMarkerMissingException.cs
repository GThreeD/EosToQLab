namespace EosToQLab.Core.Exceptions;

public sealed class CsvStartMarkerMissingException : EosToQLabException
{
    public CsvStartMarkerMissingException(string fileName)
        : base("EOS_CSV_START_MARKER_MISSING", $"The CSV file '{fileName}' does not contain a START_TARGETS marker.")
    {
    }
}