namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class CsvFollowColumnMissingWarningTests
{
    [Fact]
    public void Exposes_stable_code_warning_severity_and_message()
    {
        var warning = new CsvFollowColumnMissingWarning();

        Assert.Equal("EOS_CSV_FOLLOW_COLUMN_MISSING", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("FOLLOW", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}