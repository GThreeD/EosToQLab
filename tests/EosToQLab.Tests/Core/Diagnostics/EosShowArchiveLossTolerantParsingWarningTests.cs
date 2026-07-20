namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class EosShowArchiveLossTolerantParsingWarningTests
{
    [Fact]
    public void Exposes_stable_code_warning_severity_and_message()
    {
        var warning = new EosShowArchiveLossTolerantParsingWarning();

        Assert.Equal("EOS_SHOW_ARCHIVE_LOSS_TOLERANT_PARSE", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("loss-tolerant", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}