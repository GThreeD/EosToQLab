using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class CsvColumnBindingExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new CsvColumnBindingException("Value");

        ExceptionAssertions.HasDetails(exception, "EOS_CSV_COLUMN_BINDING_FAILED", "Value");
    }
}