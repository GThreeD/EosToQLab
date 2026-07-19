namespace EosToQLab.Core.Exceptions;

public sealed class CsvValueConversionException : EosToQLabException
{
    public CsvValueConversionException(string columnName, int rowNumber, string value, Type targetType,
        Exception? innerException = null)
        : base("EOS_CSV_VALUE_CONVERSION_FAILED",
            $"The value '{value}' in column '{columnName}' at row {rowNumber} cannot be converted to {targetType.Name}.",
            innerException)
    {
    }
}