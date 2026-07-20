namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class EosShowArchiveNoCueSequenceWarningTests
{
    [Fact]
    public void Exposes_stable_code_warning_severity_and_message()
    {
        var warning = new EosShowArchiveNoCueSequenceWarning();

        Assert.Equal("EOS_SHOW_ARCHIVE_NO_CUE_SEQUENCE", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("monotonic", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}