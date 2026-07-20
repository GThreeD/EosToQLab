namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class EosShowArchiveFollowNotDecodedWarningTests
{
    [Fact]
    public void Exposes_stable_code_warning_severity_and_message()
    {
        var warning = new EosShowArchiveFollowNotDecodedWarning("3");

        Assert.Equal("EOS_SHOW_ARCHIVE_FOLLOW_NOT_DECODED", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("3", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}