namespace EosToQLab.Core.Import;

public static class EosShowArchiveCompatibility
{
    public const string TestedFormat = "{CB82CC14-5598-4DB1-A1D7-EBC3BE1D1038}";

    public static IReadOnlyList<(string Format, string Version)> TestedArchives { get; } =
    [
        (TestedFormat, "3.3.5.69")
    ];

    public static bool IsTested(string? format, string? version)
    {
        var normalizedFormat = format?.Trim();
        var normalizedVersion = version?.Trim();
        return TestedArchives.Any(candidate =>
            string.Equals(candidate.Format, normalizedFormat, StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidate.Version, normalizedVersion, StringComparison.OrdinalIgnoreCase));
    }
}
