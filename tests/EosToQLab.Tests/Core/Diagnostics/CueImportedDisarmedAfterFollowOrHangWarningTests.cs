namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class CueImportedDisarmedAfterFollowOrHangWarningTests
{
    [Fact]
    public void Exposes_stable_code_warning_severity_and_message()
    {
        var warning = new CueImportedDisarmedAfterFollowOrHangWarning("1/2");

        Assert.Equal("EOS_FOLLOWED_CUE_DISARMED", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("1/2", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}