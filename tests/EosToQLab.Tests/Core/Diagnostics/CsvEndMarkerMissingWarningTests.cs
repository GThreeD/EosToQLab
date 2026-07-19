namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class CsvEndMarkerMissingWarningTests
{
    [Fact]
    public void Exposes_stable_code_warning_severity_and_message()
    {
        var warning = new CsvEndMarkerMissingWarning();

        Assert.Equal("EOS_CSV_END_MARKER_MISSING", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("END_TARGETS", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}