using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class CsvUnterminatedQuotedFieldExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        var exception = new CsvUnterminatedQuotedFieldException();

        ExceptionAssertions.HasDetails(exception, "EOS_CSV_UNTERMINATED_QUOTED_FIELD", "unterminated");
    }
}