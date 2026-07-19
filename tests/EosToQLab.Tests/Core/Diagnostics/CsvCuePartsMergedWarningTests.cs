namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class CsvCuePartsMergedWarningTests
{
    [Fact]
    public void Exposes_stable_code_warning_severity_and_message()
    {
        var warning = new CsvCuePartsMergedWarning(3);

        Assert.Equal("EOS_CSV_CUE_PARTS_MERGED", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("3", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}