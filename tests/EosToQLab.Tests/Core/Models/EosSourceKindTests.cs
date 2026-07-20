namespace EosToQLab.Tests.Core.Models;

public sealed class EosSourceKindTests
{
    [Fact]
    public void Defines_csv_and_show_archive_sources()
    {
        Assert.Equal([EosSourceKind.Csv, EosSourceKind.ShowArchive], Enum.GetValues<EosSourceKind>());
    }
}
