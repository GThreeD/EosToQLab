namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class CsvCueWithoutTargetIdWarningTests
{
    [Fact]
    public void Exposes_stable_code_warning_severity_and_message()
    {
        var warning = new CsvCueWithoutTargetIdWarning(9);

        Assert.Equal("EOS_CSV_CUE_WITHOUT_TARGET_ID", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("9", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}