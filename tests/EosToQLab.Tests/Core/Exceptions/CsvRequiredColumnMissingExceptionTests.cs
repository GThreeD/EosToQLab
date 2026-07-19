using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Exceptions;

public sealed class CsvRequiredColumnMissingExceptionTests
{
    [Fact]
    public void Constructor_exposes_stable_diagnostic_details()
    {
        IReadOnlyCollection<string> missing = ["A", "B"];
        var exception = new CsvRequiredColumnMissingException("show.csv", missing);

        ExceptionAssertions.HasDetails(exception, "EOS_CSV_REQUIRED_COLUMN_MISSING", "show.csv", "A, B");
        Assert.Same(missing, exception.MissingColumns);
    }
}