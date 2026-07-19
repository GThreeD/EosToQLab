using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class CsvValueConversionExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new CsvValueConversionException("COUNT", 7, "bad", typeof(int), inner);

        ExceptionAssertions.HasDetails(exception, "EOS_CSV_VALUE_CONVERSION_FAILED", "COUNT", "7", "bad", "Int32");
    }
}