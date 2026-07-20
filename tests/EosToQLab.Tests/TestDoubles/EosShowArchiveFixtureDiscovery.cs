namespace EosToQLab.Tests.TestDoubles;

internal sealed record EosShowArchiveFixtureCase(
    string Name,
    string ArchivePath,
    string ExpectedPath);

internal static class EosShowArchiveFixtureDiscovery
{
    public static IReadOnlyList<EosShowArchiveFixtureCase> Discover(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        var root = TestData.FixturePath("EosShowArchive", category);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"EOS show archive fixture category was not found: {root}");

        var caseDirectories = Directory
            .EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => IsShowArchiveExtension(Path.GetExtension(path))
                           || string.Equals(Path.GetFileName(path), "expected.json", StringComparison.Ordinal))
            .Select(path => Path.GetDirectoryName(path)!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (caseDirectories.Length == 0)
            throw new InvalidDataException($"No EOS show archive fixture cases were found below '{root}'.");

        return caseDirectories.Select(directory => CreateCase(root, directory)).ToArray();
    }

    public static TheoryData<string, string, string> CreateTheoryData(string category)
    {
        var data = new TheoryData<string, string, string>();
        foreach (var fixture in Discover(category))
            data.Add(fixture.Name, fixture.ArchivePath, fixture.ExpectedPath);

        return data;
    }

    private static EosShowArchiveFixtureCase CreateCase(string root, string directory)
    {
        var archives = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
            .Where(path => IsShowArchiveExtension(Path.GetExtension(path)))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var contracts = Directory.EnumerateFiles(directory, "expected.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var name = Path.GetRelativePath(root, directory).Replace(Path.DirectorySeparatorChar, '/');

        if (archives.Length != 1 || contracts.Length != 1)
            throw new InvalidDataException(
                $"EOS show archive fixture '{name}' must contain exactly one .esf2 or .esf3d archive and one expected.json contract.");

        return new EosShowArchiveFixtureCase(name, archives[0], contracts[0]);
    }

    private static bool IsShowArchiveExtension(string extension)
    {
        return string.Equals(extension, ".esf2", StringComparison.OrdinalIgnoreCase)
               || string.Equals(extension, ".esf3d", StringComparison.OrdinalIgnoreCase);
    }
}