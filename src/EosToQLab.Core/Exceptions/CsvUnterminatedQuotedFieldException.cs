namespace EosToQLab.Core.Exceptions;

public sealed class CsvUnterminatedQuotedFieldException : EosToQLabException
{
    public CsvUnterminatedQuotedFieldException()
        : base("EOS_CSV_UNTERMINATED_QUOTED_FIELD", "The CSV file contains an unterminated quoted field.") { }
}
