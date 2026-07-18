namespace EosToQLab.Core.Exceptions;

public sealed class CsvColumnBindingException : EosToQLabException
{
    public CsvColumnBindingException(string propertyName)
        : base("EOS_CSV_COLUMN_BINDING_FAILED", $"CSV property '{propertyName}' does not define a valid EOS column binding.") { }
}
