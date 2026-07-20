namespace EosToQLab.Core.Import;

public interface IEosShowArchiveCompatibility
{
    bool IsCovered(string? format, string? version);
}