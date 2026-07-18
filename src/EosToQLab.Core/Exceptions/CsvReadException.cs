namespace EosToQLab.Core.Exceptions;

public sealed class CsvReadException : EosToQLabException
{
    public CsvReadException(string fileName, Exception innerException)
        : base("EOS_CSV_READ_FAILED", $"The CSV file '{fileName}' could not be read.", innerException) { }
}
