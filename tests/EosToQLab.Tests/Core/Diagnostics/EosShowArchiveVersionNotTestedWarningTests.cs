namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class EosShowArchiveVersionNotTestedWarningTests
{
    [Fact]
    public void Describes_an_unknown_archive_version_with_stable_diagnostic_metadata()
    {
        var warning = new EosShowArchiveVersionNotTestedWarning("format-id", "4.0.0.1");

        Assert.Equal("EOS_SHOW_ARCHIVE_VERSION_NOT_TESTED", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("format-id", warning.Message, StringComparison.Ordinal);
        Assert.Contains("4.0.0.1", warning.Message, StringComparison.Ordinal);
        Assert.Contains("compatibility fixtures", warning.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("may be incompatible", warning.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Explains_when_version_json_cannot_be_read()
    {
        var warning = new EosShowArchiveVersionNotTestedWarning(null, null);

        Assert.Contains("version.json", warning.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("may be incompatible", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}