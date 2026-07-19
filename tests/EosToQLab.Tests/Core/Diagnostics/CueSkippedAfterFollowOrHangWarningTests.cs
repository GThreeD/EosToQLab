namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class CueSkippedAfterFollowOrHangWarningTests
{
    [Fact]
    public void Exposes_stable_code_warning_severity_and_message()
    {
        var warning = new CueSkippedAfterFollowOrHangWarning("1/2");

        Assert.Equal("QLAB_CUE_SKIPPED_AFTER_FOLLOW_OR_HANG", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("1/2", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}