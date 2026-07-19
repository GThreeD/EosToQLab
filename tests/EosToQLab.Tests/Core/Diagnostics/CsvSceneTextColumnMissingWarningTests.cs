namespace EosToQLab.Tests.Core.Diagnostics;

public sealed class CsvSceneTextColumnMissingWarningTests
{
    [Fact]
    public void Exposes_stable_code_warning_severity_and_message()
    {
        var warning = new CsvSceneTextColumnMissingWarning();

        Assert.Equal("EOS_CSV_SCENE_TEXT_COLUMN_MISSING", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("SCENE_TEXT", warning.Message, StringComparison.OrdinalIgnoreCase);
    }
}