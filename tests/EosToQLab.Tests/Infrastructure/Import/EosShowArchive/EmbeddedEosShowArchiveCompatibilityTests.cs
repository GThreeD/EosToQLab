using System.Reflection;
using System.Text;
using System.Text.Json;
using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Infrastructure.Import.EosShowArchive;

public sealed class EmbeddedEosShowArchiveCompatibilityTests
{
    [Fact]
    public void Loads_the_generated_catalog_and_matches_format_version_pairs_case_insensitively()
    {
        var sut = new EmbeddedEosShowArchiveCompatibility(Catalog(
            """
            {
              "schemaVersion": 1,
              "archives": [
                { "format": " format-id ", "version": " 3.3.5.69 ", "fixture": "eos-3.3.5.69" }
              ]
            }
            """));

        Assert.True(sut.IsCovered("FORMAT-ID", "3.3.5.69"));
        Assert.True(sut.IsCovered(" format-id ", " 3.3.5.69 "));
        Assert.False(sut.IsCovered("format-id", "3.3.5.70"));
        Assert.False(sut.IsCovered(null, "3.3.5.69"));
        Assert.False(sut.IsCovered("format-id", " "));
    }

    [Fact]
    public void Generated_catalog_matches_exactly_the_compatibility_fixture_contracts()
    {
        var expected = EosShowArchiveFixtureDiscovery.Discover("Compatibility")
            .Select(fixture => fixture.ExpectedPath)
            .Select(File.ReadAllText)
            .Select(text => JsonDocument.Parse(text))
            .Select(document =>
            {
                using (document)
                {
                    var archive = document.RootElement.GetProperty("archive");
                    return $"{archive.GetProperty("format").GetString()}|{archive.GetProperty("version").GetString()}";
                }
            })
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        using var catalogStream = typeof(EmbeddedEosShowArchiveCompatibility).Assembly
            .GetManifestResourceStream(EmbeddedEosShowArchiveCompatibility.ResourceName);
        Assert.NotNull(catalogStream);
        using var catalog = JsonDocument.Parse(catalogStream);
        var actual = catalog.RootElement.GetProperty("archives")
            .EnumerateArray()
            .Select(entry => $"{entry.GetProperty("format").GetString()}|{entry.GetProperty("version").GetString()}")
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Default_constructor_loads_the_build_generated_embedded_resource()
    {
        var fixture = EosShowArchiveFixtureDiscovery.Discover("Compatibility").First();
        using var contract = JsonDocument.Parse(File.ReadAllText(fixture.ExpectedPath));
        var archive = contract.RootElement.GetProperty("archive");
        var format = archive.GetProperty("format").GetString();
        var version = archive.GetProperty("version").GetString();
        var sut = new EmbeddedEosShowArchiveCompatibility();

        Assert.True(sut.IsCovered(format, version));
        Assert.False(sut.IsCovered(format, version + ".unknown"));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{ \"schemaVersion\": 2, \"archives\": [] }")]
    [InlineData("{ \"schemaVersion\": 1 }")]
    [InlineData("{ \"schemaVersion\": 1, \"archives\": {} }")]
    public void Rejects_unsupported_catalog_schemas(string json)
    {
        Assert.Throws<InvalidDataException>(() =>
            new EmbeddedEosShowArchiveCompatibility(Catalog(json)));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{ \"format\": \"x\" }")]
    [InlineData("{ \"format\": \"\", \"version\": \"1\" }")]
    [InlineData("{ \"format\": \"x\", \"version\": \" \" }")]
    public void Rejects_invalid_archive_entries(string entry)
    {
        var json = $$"""
                     {
                       "schemaVersion": 1,
                       "archives": [{{entry}}]
                     }
                     """;

        Assert.Throws<InvalidDataException>(() =>
            new EmbeddedEosShowArchiveCompatibility(Catalog(json)));
    }

    [Fact]
    public void Rejects_an_assembly_without_the_generated_catalog()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new EmbeddedEosShowArchiveCompatibility(typeof(EmbeddedEosShowArchiveCompatibilityTests).Assembly));
        Assert.Throws<ArgumentNullException>(() =>
            new EmbeddedEosShowArchiveCompatibility((Assembly)null!));
    }

    [Fact]
    public void Rejects_a_null_catalog_stream()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new EmbeddedEosShowArchiveCompatibility((Stream)null!));
    }

    private static MemoryStream Catalog(string json) => new(Encoding.UTF8.GetBytes(json));
}
