using System.Text;
using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Infrastructure.Import.Csv;

public sealed class CsvEosCueImporterTests
{
    private readonly CsvEosCueImporter _sut = new();

    [Fact]
    public void Reports_source_kind_and_supported_extension()
    {
        Assert.Equal(EosSourceKind.Csv, _sut.SourceKind);
        Assert.True(_sut.CanImport("SHOW.CSV"));
        Assert.False(_sut.CanImport("show.esf3d"));
    }

    [Fact]
    public async Task Imports_reference_fixture_and_aggregates_parts()
    {
        var path = TestData.FixturePath("Csv", "reference-eos.csv");
        await using var stream = File.OpenRead(path);
        var result = await _sut.ImportAsync(new EosImportRequest("reference-eos.csv", stream),
            TestContext.Current.CancellationToken);

        Assert.Equal(4, result.Cues.Count);
        Assert.Equal(EosSourceKind.Csv, result.SourceKind);
        Assert.Contains("reference-eos.csv", result.SourceDescription);
        Assert.Contains(result.Cues, cue => cue is { CueNumber: "1", Label: "Blackout" });
        Assert.Contains(result.Cues, cue => cue is { CueNumber: "2", Follow: "F3" });
        Assert.Contains(result.Cues, cue => cue is { CueNumber: "4", SceneText: "Szene 1" });
        Assert.Contains(result.Diagnostics, d => d is CsvCuePartsMergedWarning);
        Assert.Equal(27, result.Cues[0].AdditionalValues.Count);
    }

    [Fact]
    public async Task Handles_bom_case_insensitive_headers_duplicate_headers_and_missing_end_marker()
    {
        var csv =
            "\uFEFFSTART_TARGETS\nTARGET_TYPE_AS_TEXT,TARGET_LIST_NUMBER,TARGET_ID,TARGET_ID,LABEL,FOLLOW,SCENE_TEXT\nCue,0,001.500,ignored, Label ,F1, Scene \nChannel,1,2,ignored,X,,\n";
        var result = await Import(csv, TestContext.Current.CancellationToken);
        var cue = Assert.Single(result.Cues);
        Assert.Equal(1, cue.ListNumber);
        Assert.Equal("1.5", cue.CueNumber);
        Assert.Equal("Label", cue.Label);
        Assert.Equal("Scene", cue.SceneText);
        Assert.Contains(result.Diagnostics, d => d is CsvEndMarkerMissingWarning);
    }

    [Fact]
    public async Task Reports_missing_optional_columns_empty_ids_and_no_cues()
    {
        var csv = "START_TARGETS\nTARGET_TYPE_AS_TEXT,TARGET_LIST_NUMBER,TARGET_ID\nCue,1,\nChannel,1,2\nEND_TARGETS";
        var result = await Import(csv, TestContext.Current.CancellationToken);
        Assert.Empty(result.Cues);
        Assert.Contains(result.Diagnostics, d => d is CsvFollowColumnMissingWarning);
        Assert.Contains(result.Diagnostics, d => d is CsvSceneTextColumnMissingWarning);
        Assert.Contains(result.Diagnostics, d => d is CsvCueWithoutTargetIdWarning);
        Assert.Contains(result.Diagnostics, d => d is NoCuesFoundWarning);
    }

    [Fact]
    public async Task Uses_dcid_to_separate_same_number_and_falls_back_to_first_part()
    {
        var csv =
            "START_TARGETS\nTARGET_TYPE_AS_TEXT,TARGET_LIST_NUMBER,TARGET_ID,TARGET_DCID,PART_NUMBER,LABEL,FOLLOW,SCENE_TEXT\n" +
            "Cue,2,1,A,1,Part A,,\nCue,2,1,B,1,Part B,,\nEND_TARGETS";
        var result = await Import(csv, TestContext.Current.CancellationToken);
        Assert.Equal(2, result.Cues.Count);
        Assert.Equal(["A", "B"], result.Cues.Select(c => c.TargetDcid));
    }

    [Theory]
    [InlineData("anything")]
    [InlineData("START_TARGETS")]
    public async Task Throws_for_missing_structure(string csv)
    {
        if (csv == "anything")
            await Assert.ThrowsAsync<CsvStartMarkerMissingException>(() =>
                Import(csv, TestContext.Current.CancellationToken));
        else
            await Assert.ThrowsAsync<CsvHeaderMissingException>(() =>
                Import(csv, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Throws_for_missing_required_column()
    {
        var exception = await Assert.ThrowsAsync<CsvRequiredColumnMissingException>(() =>
            Import("START_TARGETS\nTARGET_ID\n1\nEND_TARGETS", TestContext.Current.CancellationToken));
        Assert.NotEmpty(exception.MissingColumns);
    }

    [Fact]
    public async Task Wraps_stream_read_errors_and_rejects_null_request()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.ImportAsync(null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<CsvReadException>(() =>
            _sut.ImportAsync(new EosImportRequest("show.csv", new ThrowingStream(new IOException("broken"))),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Honors_cancellation_while_processing_rows()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Import(
            "START_TARGETS\nTARGET_TYPE_AS_TEXT,TARGET_LIST_NUMBER,TARGET_ID\nCue,1,1\nEND_TARGETS",
            cancellation.Token));
    }

    private async Task<EosImportResult> Import(string csv, CancellationToken cancellationToken = default)
    {
        var bytes = Encoding.UTF8.GetBytes(csv);
        await using var stream = new MemoryStream(bytes);
        return await _sut.ImportAsync(new EosImportRequest("show.csv", stream), cancellationToken);
    }
}