namespace EosToQLab.Core.Exceptions;

public sealed class CsvRequiredColumnMissingException : EosToQLabException
{
    public CsvRequiredColumnMissingException(string fileName, IReadOnlyCollection<string> missingColumns)
        : base("EOS_CSV_REQUIRED_COLUMN_MISSING",
            $"The CSV file '{fileName}' is missing required columns: {string.Join(", ", missingColumns)}")
    {
        MissingColumns = missingColumns;
    }

    public IReadOnlyCollection<string> MissingColumns { get; }
}