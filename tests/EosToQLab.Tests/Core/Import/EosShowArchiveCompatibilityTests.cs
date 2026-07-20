using System.IO.Compression;
using System.Text.Json;
using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Import;

public sealed class EosShowArchiveCompatibilityTests
{
    [Theory]
    [InlineData("{CB82CC14-5598-4DB1-A1D7-EBC3BE1D1038}", "3.3.5.69", true)]
    [InlineData(" {cb82cc14-5598-4db1-a1d7-ebc3be1d1038} ", " 3.3.5.69 ", true)]
    [InlineData("{CB82CC14-5598-4DB1-A1D7-EBC3BE1D1038}", "3.3.5.70", false)]
    [InlineData("{00000000-0000-0000-0000-000000000000}", "3.3.5.69", false)]
    [InlineData(null, null, false)]
    public void Identifies_only_versions_covered_by_the_compatibility_corpus(
        string? format,
        string? version,
        bool expected)
    {
        Assert.Equal(expected, EosShowArchiveCompatibility.IsTested(format, version));
    }

    [Fact]
    public async Task Every_declared_tested_version_has_a_versioned_golden_fixture()
    {
        var fixtureRoot = TestData.FixturePath("Esf3d");
        var manifests = new HashSet<(string Format, string Version)>();

        foreach (var archivePath in Directory.EnumerateFiles(fixtureRoot, "*.*", SearchOption.AllDirectories)
                     .Where(path => IsShowArchiveExtension(Path.GetExtension(path))))
        {
            await using var stream = File.OpenRead(archivePath);
            await using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var versionEntry = archive.Entries.FirstOrDefault(entry =>
                entry.FullName.EndsWith("version.json", StringComparison.OrdinalIgnoreCase));
            if (versionEntry is null) continue;

            await using var input = await versionEntry.OpenAsync(TestContext.Current.CancellationToken);
            using var document =
                await JsonDocument.ParseAsync(input, cancellationToken: TestContext.Current.CancellationToken);
            manifests.Add((
                document.RootElement.GetProperty("Format").GetString()!,
                document.RootElement.GetProperty("Version").GetString()!));
        }

        var declared = EosShowArchiveCompatibility.TestedArchives.ToHashSet();

        Assert.Equal(
            declared.OrderBy(item => item.Version, StringComparer.Ordinal),
            manifests.OrderBy(item => item.Version, StringComparer.Ordinal));
        Assert.All(manifests, manifest =>
            Assert.True(EosShowArchiveCompatibility.IsTested(manifest.Format, manifest.Version)));
    }

    private static bool IsShowArchiveExtension(string extension)
    {
        return string.Equals(extension, ".esf2", StringComparison.OrdinalIgnoreCase)
               || string.Equals(extension, ".esf3d", StringComparison.OrdinalIgnoreCase);
    }
}