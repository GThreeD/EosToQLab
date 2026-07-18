namespace EosToQLab.Core.Exceptions;

public sealed class CsvHeaderMissingException : EosToQLabException
{
    public CsvHeaderMissingException(string fileName)
        : base("EOS_CSV_HEADER_MISSING", $"The CSV file '{fileName}' does not contain a header row after START_TARGETS.") { }
}
