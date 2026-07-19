using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Infrastructure.Import;

public sealed class EosCueImporterFactoryTests
{
    [Fact]
    public void Selects_first_matching_importer()
    {
        var first = new FakeEosCueImporter(EosSourceKind.Csv,
            name => name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
        var second = new FakeEosCueImporter(EosSourceKind.Esf3d, _ => true);
        var factory = new EosCueImporterFactory([first, second]);

        Assert.Same(first, factory.CreateFor("show.csv"));
        Assert.Same(second, factory.CreateFor("show.esf3d"));
    }

    [Fact]
    public void Throws_for_unsupported_file()
    {
        Assert.Throws<UnsupportedEosImportFormatException>(() => new EosCueImporterFactory([]).CreateFor("show.txt"));
    }
}