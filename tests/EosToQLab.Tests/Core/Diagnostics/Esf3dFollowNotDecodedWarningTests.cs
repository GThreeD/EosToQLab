namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class Esf3dFollowNotDecodedWarningTests
{
    [Fact]
    public void Exposes_stable_code_warning_severity_and_message()
    {
        var warning = new Esf3dFollowNotDecodedWarning("3");

        Assert.Equal("EOS_ESF3D_FOLLOW_NOT_DECODED", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("3", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}