namespace EosToQLab.Tests.Core.Models;

public sealed class EosImportResultTests
{
    [Fact]
    public void Stores_constructor_values()
    {
        var value = new EosImportResult([], [], EosSourceKind.Csv, "source");
        Assert.Empty(value.Cues);
        Assert.Empty(value.Diagnostics);
        Assert.Equal(EosSourceKind.Csv, value.SourceKind);
        Assert.Equal("source", value.SourceDescription);
    }
}