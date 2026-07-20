using System.IO.Compression;
using System.Text.Json;
using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Infrastructure.Import.EosShowArchive;

public sealed class EosShowArchiveCueImporterTests
{
    private readonly EosShowArchiveCueImporter _sut = new(new FakeEosShowArchiveCompatibility());

    public static TheoryData<string, string, string> CompatibilityFixtures =>
        EosShowArchiveFixtureDiscovery.CreateTheoryData("Compatibility");

    public static TheoryData<string, string, string> RegressionFixtures =>
        EosShowArchiveFixtureDiscovery.CreateTheoryData("Regression");

    [Fact]
    public void Constructor_rejects_a_null_compatibility_service()
    {
        Assert.Throws<ArgumentNullException>(() => new EosShowArchiveCueImporter(null!));
    }

    [Fact]
    public void Reports_source_kind_and_supported_extensions()
    {
        Assert.Equal(EosSourceKind.ShowArchive, _sut.SourceKind);
        Assert.True(_sut.CanImport("SHOW.ESF3D"));
        Assert.True(_sut.CanImport("SHOW.ESF2"));
        Assert.False(_sut.CanImport("show.csv"));
    }

    [Fact]
    public void Discovers_fixture_cases_from_their_directories_without_known_archive_names()
    {
        Assert.NotEmpty(EosShowArchiveFixtureDiscovery.Discover("Compatibility"));
        Assert.NotEmpty(EosShowArchiveFixtureDiscovery.Discover("Regression"));
    }

    [Theory]
    [MemberData(nameof(CompatibilityFixtures))]
    public async Task Matches_every_compatibility_contract(
        string caseName,
        string archivePath,
        string expectedPath)
    {
        Assert.False(string.IsNullOrWhiteSpace(caseName));
        var sut = new EosShowArchiveCueImporter(new EmbeddedEosShowArchiveCompatibility());

        var result = await EosShowArchiveContractAssertions.ImportAndMatchAsync(
            sut,
            archivePath,
            expectedPath);

        Assert.DoesNotContain(
            result.Diagnostics,
            diagnostic => diagnostic is EosShowArchiveVersionNotTestedWarning);
    }

    [Theory]
    [MemberData(nameof(RegressionFixtures))]
    public async Task Matches_every_regression_contract(
        string caseName,
        string archivePath,
        string expectedPath)
    {
        Assert.False(string.IsNullOrWhiteSpace(caseName));

        await EosShowArchiveContractAssertions.ImportAndMatchAsync(
            _sut,
            archivePath,
            expectedPath);
    }

    [Theory]
    [MemberData(nameof(CompatibilityFixtures))]
    public async Task Imports_compatibility_payloads_as_esf2_without_optional_3d_data(
        string caseName,
        string archivePath,
        string expectedPath)
    {
        Assert.False(string.IsNullOrWhiteSpace(caseName));
        await using var fixtureStream = File.OpenRead(archivePath);
        await using var fixtureArchive = new ZipArchive(fixtureStream, ZipArchiveMode.Read);
        await using var stream = await CreateArchiveFromEntriesAsync(
            fixtureArchive,
            "showdat.dat",
            "version.json");
        using var expected = JsonDocument.Parse(
            await File.ReadAllTextAsync(expectedPath, TestContext.Current.CancellationToken));
        var archiveContract = expected.RootElement.GetProperty("archive");
        var expectedFormat = archiveContract.GetProperty("format").GetString();
        var expectedVersion = archiveContract.GetProperty("version").GetString();
        var expectedCueCount = expected.RootElement.GetProperty("cues").GetArrayLength();

        var sut = new EosShowArchiveCueImporter(new EmbeddedEosShowArchiveCompatibility());
        var result = await sut.ImportAsync(
            new EosImportRequest("fixture.esf2", stream),
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedCueCount, result.Cues.Count);
        Assert.StartsWith("ESF2:", result.SourceDescription);
        Assert.Contains($"EOS {expectedVersion}", result.SourceDescription, StringComparison.Ordinal);
        Assert.DoesNotContain(
            result.Diagnostics,
            diagnostic => diagnostic is EosShowArchiveVersionNotTestedWarning);
        Assert.All(result.Cues, cue =>
        {
            Assert.Equal(expectedFormat, cue.AdditionalValues["EOS_ARCHIVE_FORMAT"]);
            Assert.Equal(expectedVersion, cue.AdditionalValues["EOS_ARCHIVE_VERSION"]);
        });
    }

    [Fact]
    public async Task Warns_when_archive_version_is_not_in_the_fixture_derived_catalog()
    {
        await using var archive = CreateArchiveWithVersion(
            [],
            """{ "Format": "future-format", "Version": "9.9.9.1" }""");

        var sut = new EosShowArchiveCueImporter(new FakeEosShowArchiveCompatibility((_, _) => false));
        var result = await sut.ImportAsync(
            new EosImportRequest("future.esf3d", archive),
            TestContext.Current.CancellationToken);

        var warning = Assert.Single(result.Diagnostics.OfType<EosShowArchiveVersionNotTestedWarning>());
        Assert.Equal("future-format", warning.Format);
        Assert.Equal("9.9.9.1", warning.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("[]")]
    public async Task Warns_when_version_manifest_is_missing_or_unreadable(string? versionJson)
    {
        await using var archive = versionJson is null
            ? CreateArchive([])
            : CreateArchiveWithVersion([], versionJson);

        var sut = new EosShowArchiveCueImporter(new FakeEosShowArchiveCompatibility((_, _) => false));
        var result = await sut.ImportAsync(
            new EosImportRequest("unknown.esf2", archive),
            TestContext.Current.CancellationToken);

        var warning = Assert.Single(result.Diagnostics.OfType<EosShowArchiveVersionNotTestedWarning>());
        Assert.Null(warning.Format);
        Assert.Null(warning.Version);
        Assert.StartsWith("ESF2:", result.SourceDescription);
    }

    [Fact]
    public async Task Ignores_a_number_marker_with_an_invalid_cue_record_trailer()
    {
        byte[] showData =
        [
            0x02, 0x01, 0x00, 0x01, 0x01,
            0x09, 0x10, 0x27,
            0x00, 0x02, 0x02, 0x02, 0x01, 0x00, 0x00, 0x02,
            0x08,
            0x04, 0x04, 0x04,
            0x02, 0x01, 0x00, 0x01, 0x01,
            0x08, 0x01
        ];
        await using var archive = CreateArchive(showData);

        var result = await _sut.ImportAsync(
            new EosImportRequest("invalid-trailer.esf3d", archive),
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Cues);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic is EosShowArchiveNoCueSequenceWarning);
    }

    [Fact]
    public async Task Empty_show_data_returns_diagnostics_instead_of_guessing()
    {
        await using var archive = CreateArchive([]);
        var result = await _sut.ImportAsync(
            new EosImportRequest("empty.esf3d", archive),
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Cues);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic is EosShowArchiveNoCueSequenceWarning);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic is NoCuesFoundWarning);
    }

    [Theory]
    [MemberData(nameof(CompatibilityFixtures))]
    public async Task Chooses_the_shallowest_showdat_entry_case_insensitively(
        string caseName,
        string archivePath,
        string expectedPath)
    {
        Assert.False(string.IsNullOrWhiteSpace(caseName));
        Assert.True(File.Exists(expectedPath));
        await using var fixtureStream = File.OpenRead(archivePath);
        await using var source = new ZipArchive(fixtureStream, ZipArchiveMode.Read);
        var showData = FindShallowestEntry(source, "showdat.dat");
        await using var input = await showData.OpenAsync(TestContext.Current.CancellationToken);
        using var payload = new MemoryStream();
        await input.CopyToAsync(payload, TestContext.Current.CancellationToken);
        await using var archive = CreateArchive(payload.ToArray(), "SHOWDAT.DAT", "nested/showdat.dat");

        var result = await _sut.ImportAsync(
            new EosImportRequest("show.esf3d", archive),
            TestContext.Current.CancellationToken);

        Assert.Contains("(SHOWDAT.DAT)", result.SourceDescription);
    }

    [Fact]
    public async Task Rejects_null_invalid_archives_missing_entries_and_read_errors()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.ImportAsync(null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<EosShowArchiveInvalidException>(() =>
            _sut.ImportAsync(
                new EosImportRequest("bad.esf3d", new MemoryStream([1, 2, 3])),
                TestContext.Current.CancellationToken));
        await using var noEntry = CreateArchive([], "other.dat");
        await Assert.ThrowsAsync<ShowDataEntryMissingException>(() =>
            _sut.ImportAsync(
                new EosImportRequest("missing.esf3d", noEntry),
                TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ShowDataReadException>(() =>
            _sut.ImportAsync(
                new EosImportRequest("broken.esf3d", new ThrowingStream(new IOException("broken"))),
                TestContext.Current.CancellationToken));
    }

    private static MemoryStream CreateArchiveWithVersion(byte[] showData, string versionJson)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            var showDataEntry = archive.CreateEntry("showdat.dat");
            using (var output = showDataEntry.Open())
            {
                output.Write(showData);
            }

            var versionEntry = archive.CreateEntry("version.json");
            using var writer = new StreamWriter(versionEntry.Open());
            writer.Write(versionJson);
        }

        stream.Position = 0;
        return stream;
    }

    private static async Task<MemoryStream> CreateArchiveFromEntriesAsync(
        ZipArchive source,
        params string[] entryNames)
    {
        var stream = new MemoryStream();
        using (var destination = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var entryName in entryNames)
            {
                var sourceEntry = FindShallowestEntry(source, entryName);
                var destinationEntry = destination.CreateEntry(entryName);
                await using var input = await sourceEntry.OpenAsync(TestContext.Current.CancellationToken);
                await using var output = await destinationEntry.OpenAsync(TestContext.Current.CancellationToken);
                await input.CopyToAsync(output, TestContext.Current.CancellationToken);
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static ZipArchiveEntry FindShallowestEntry(ZipArchive archive, string fileName)
    {
        return archive.Entries
                   .Where(entry => entry.FullName.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                   .OrderBy(entry => entry.FullName.Count(character => character == '/'))
                   .FirstOrDefault()
               ?? throw new InvalidDataException($"Archive entry '{fileName}' was not found.");
    }

    private static MemoryStream CreateArchive(byte[] showData, params string[] names)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var name in names.Length == 0 ? ["showdat.dat"] : names)
            {
                var entry = archive.CreateEntry(name);
                using var output = entry.Open();
                output.Write(showData);
            }
        }

        stream.Position = 0;
        return stream;
    }
}
