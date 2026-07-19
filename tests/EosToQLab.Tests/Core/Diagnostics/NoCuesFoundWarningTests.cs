namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class NoCuesFoundWarningTests
{
    [Fact]
    public void Exposes_stable_code_warning_severity_and_message()
    {
        var warning = new NoCuesFoundWarning();

        Assert.Equal("EOS_NO_CUES_FOUND", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("No EOS cue", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}