namespace EosToQLab.Core.Import;

public interface IEosCueImporterFactory
{
    IEosCueImporter CreateFor(string fileName);
}