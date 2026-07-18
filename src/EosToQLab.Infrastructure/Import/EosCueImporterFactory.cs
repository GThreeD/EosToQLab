using EosToQLab.Core.Exceptions;
using EosToQLab.Core.Import;

namespace EosToQLab.Infrastructure.Import;

public sealed class EosCueImporterFactory(IEnumerable<IEosCueImporter> importers) : IEosCueImporterFactory
{
    private readonly IReadOnlyList<IEosCueImporter> _importers = importers.ToArray();

    public IEosCueImporter CreateFor(string fileName) =>
        _importers.FirstOrDefault(importer => importer.CanImport(fileName))
        ?? throw new UnsupportedEosImportFormatException(fileName);
}
