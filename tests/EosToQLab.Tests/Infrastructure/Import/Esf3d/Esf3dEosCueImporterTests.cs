using System.IO.Compression;
using System.Text.Json;
using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Infrastructure.Import.Esf3d;

public sealed class Esf3dEosCueImporterTests
{
    private readonly Esf3dEosCueImporter _sut = new();

    public static TheoryData<string, string> CompatibilityFixtures
    {
        get
        {
            var fixtures = new TheoryData<string, string>();
            var root = TestData.FixturePath("Esf3d");
            foreach (var directory in Directory.EnumerateDirectories(root)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                var expectedPath = Path.Combine(directory, "expected.json");
                var archivePath = Directory.EnumerateFiles(directory, "*.esf3d").SingleOrDefault();
                if (File.Exists(expectedPath) && archivePath is not null) fixtures.Add(archivePath, expectedPath);
            }

            return fixtures;
        }
    }

    [Fact]
    public void Reports_source_kind_and_supported_extension()
    {
        Assert.Equal(EosSourceKind.Esf3d, _sut.SourceKind);
        Assert.True(_sut.CanImport("SHOW.ESF3D"));
        Assert.False(_sut.CanImport("show.csv"));
    }

    [Fact]
    public void Every_versioned_fixture_directory_has_one_archive_and_one_contract()
    {
        var root = TestData.FixturePath("Esf3d");
        var directories = Directory.EnumerateDirectories(root).ToArray();
        Assert.NotEmpty(directories);

        var incomplete = directories
            .Where(directory =>
                Directory.EnumerateFiles(directory, "*.esf3d").Count() != 1
                || !File.Exists(Path.Combine(directory, "expected.json")))
            .Select(Path.GetFileName)
            .ToArray();

        Assert.Empty(incomplete);
    }

    [Theory]
    [MemberData(nameof(CompatibilityFixtures))]
    public async Task Matches_every_versioned_golden_contract(string fixture, string expectedPath)
    {
        await using var stream = File.OpenRead(fixture);
        using var expected =
            JsonDocument.Parse(await File.ReadAllTextAsync(expectedPath, TestContext.Current.CancellationToken));

        var result = await _sut.ImportAsync(new EosImportRequest(Path.GetFileName(fixture), stream),
            TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrWhiteSpace(expected.RootElement.GetProperty("fixtureVersion").GetString()));
        var expectedCues = expected.RootElement.GetProperty("cues").EnumerateArray().ToArray();

        Assert.Equal(expectedCues.Length, result.Cues.Count);
        for (var index = 0; index < expectedCues.Length; index++)
        {
            var contract = expectedCues[index];
            var actual = result.Cues[index];
            Assert.Equal(contract.GetProperty("sourceOrder").GetInt32(), actual.SourceOrder);
            Assert.Equal(contract.GetProperty("listNumber").GetInt32(), actual.ListNumber);
            Assert.Equal(contract.GetProperty("cueNumber").GetString(), actual.CueNumber);
            Assert.Equal(GetOptional(contract, "label"), actual.Label);
            Assert.Equal(GetOptional(contract, "notes"), actual.CueNotes);
            Assert.Equal(GetOptional(contract, "scene"), actual.SceneText);
            Assert.Equal(GetOptional(contract, "follow"), actual.Follow);
            Assert.Equal(EosSourceKind.Esf3d, actual.SourceKind);
        }

        Assert.Contains(result.Diagnostics, d => d is Esf3dLossTolerantParsingWarning);
        Assert.DoesNotContain(result.Diagnostics, d => d is Esf3dFollowNotDecodedWarning);
        Assert.Contains("showdat.dat", result.SourceDescription, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Ignores_effect_reference_records_that_reuse_cue_numbers_901_to_903()
    {
        var fixture = TestData.FixturePath(
            "Esf3d",
            "known-3.3.5_Build_69",
            "synthetic-known-3.3.5_Build_69.esf3d");
        await using var stream = File.OpenRead(fixture);

        var result = await _sut.ImportAsync(new EosImportRequest(Path.GetFileName(fixture), stream),
            TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "1", "2", "3", "4", "5", "6", "7" },
            result.Cues.Select(cue => cue.CueNumber));
        Assert.DoesNotContain(result.Cues, cue => cue.CueNumber is "901" or "902" or "903");
    }

    [Theory]
    [InlineData("known-3.3.5_Build_69-with-901", null)]
    [InlineData("known-3.3.5_Build_69-with-901-label", "With Label")]
    public async Task Keeps_a_real_cue_901_and_does_not_attach_global_effect_text(
        string fixtureDirectory,
        string? expectedLabel)
    {
        var fixture = Directory.EnumerateFiles(TestData.FixturePath("Esf3d", fixtureDirectory), "*.esf3d").Single();
        await using var stream = File.OpenRead(fixture);

        var result = await _sut.ImportAsync(new EosImportRequest(Path.GetFileName(fixture), stream),
            TestContext.Current.CancellationToken);
        var cue901 = Assert.Single(result.Cues, cue => cue.CueNumber == "901");

        Assert.Equal(expectedLabel, cue901.Label);
        Assert.Null(cue901.CueNotes);
        Assert.Null(cue901.SceneText);
        Assert.DoesNotContain(result.Cues, cue => cue.CueNumber is "902" or "903");
    }

    [Fact]
    public async Task Keeps_header_valid_cues_when_only_some_records_have_the_canonical_trailer()
    {
        var fixture = TestData.FixturePath(
            "Esf3d",
            "dense-cue-record-variants-3.3.5.esf3d");
        await using var stream = File.OpenRead(fixture);

        var result = await _sut.ImportAsync(
            new EosImportRequest(Path.GetFileName(fixture), stream),
            TestContext.Current.CancellationToken);

        string[] expectedCueNumbers =
        [
            "1", "2", "3", "4",
            "10", "11", "12",
            "20", "21", "21.5", "22", "23",
            "30", "31", "32",
            "40", "40.1", "40.2", "41", "42", "43", "44", "45", "46",
            "80", "81", "82", "83", "83.1", "83.2", "83.3", "84", "85",
            "90", "91", "92", "93",
            "100", "101"
        ];

        Assert.Equal(expectedCueNumbers, result.Cues.Select(cue => cue.CueNumber));
        Assert.Contains(result.Cues, cue => cue.CueNumber == "42");
        Assert.Contains(result.Cues, cue => cue.CueNumber == "46");
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

        var result = await _sut.ImportAsync(new EosImportRequest("invalid-trailer.esf3d", archive),
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Cues);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic is Esf3dNoCueSequenceWarning);
    }

    [Fact]
    public async Task Empty_show_data_returns_diagnostics_instead_of_guessing()
    {
        await using var archive = CreateArchive([]);
        var result = await _sut.ImportAsync(new EosImportRequest("empty.esf3d", archive),
            TestContext.Current.CancellationToken);
        Assert.Empty(result.Cues);
        Assert.Contains(result.Diagnostics, d => d is Esf3dNoCueSequenceWarning);
        Assert.Contains(result.Diagnostics, d => d is NoCuesFoundWarning);
    }

    [Fact]
    public async Task Chooses_the_shallowest_showdat_entry_case_insensitively()
    {
        var bytes = await File.ReadAllBytesAsync(
            TestData.FixturePath("Esf3d", "known-3.3.5_Build_69", "synthetic-known-3.3.5_Build_69.esf3d"),
            TestContext.Current.CancellationToken);
        await using var source = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var showData =
            source.Entries.Single(e => e.FullName.EndsWith("showdat.dat", StringComparison.OrdinalIgnoreCase));
        await using var input = await showData.OpenAsync(TestContext.Current.CancellationToken);
        using var payload = new MemoryStream();
        await input.CopyToAsync(payload, TestContext.Current.CancellationToken);
        await using var archive = CreateArchive(payload.ToArray(), "SHOWDAT.DAT", "nested/showdat.dat");
        var result = await _sut.ImportAsync(new EosImportRequest("show.esf3d", archive),
            TestContext.Current.CancellationToken);
        Assert.Contains("(SHOWDAT.DAT)", result.SourceDescription);
    }

    [Fact]
    public async Task Rejects_null_invalid_archives_missing_entries_and_read_errors()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.ImportAsync(null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<Esf3dArchiveInvalidException>(() =>
            _sut.ImportAsync(new EosImportRequest("bad.esf3d", new MemoryStream([1, 2, 3])),
                TestContext.Current.CancellationToken));
        await using var noEntry = CreateArchive([], "other.dat");
        await Assert.ThrowsAsync<ShowDataEntryMissingException>(() =>
            _sut.ImportAsync(new EosImportRequest("missing.esf3d", noEntry), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ShowDataReadException>(() =>
            _sut.ImportAsync(new EosImportRequest("broken.esf3d", new ThrowingStream(new IOException("broken"))),
                TestContext.Current.CancellationToken));
    }

    private static string? GetOptional(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) ? value.GetString() : null;
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