using EosToQLab.Core.Diagnostics;
using EosToQLab.Core.Import;
using EosToQLab.Core.Models;
using EosToQLab.Core.Planning;
using EosToQLab.Infrastructure.Import;
using EosToQLab.Infrastructure.Import.Csv;
using EosToQLab.Infrastructure.Import.Esf3d;

var tests = new List<(string Name, Func<Task> Run)>
{
    ("Factory selects CSV and ESF3D strategies", TestFactoryAsync),
    ("Reference EOS CSV maps into the common cue model", TestCsvAsync),
    ("Synthetic ESF3D maps into the common cue model", TestEsf3dAsync),
    ("Follow/hang skips exactly one following cue", TestFollowLogicAsync),
    ("Scene text creates memo cues without duplication", TestSceneTextAsync)
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS  {test.Name}");
    }
    catch (Exception exception)
    {
        failures.Add($"{test.Name}: {exception.Message}");
        Console.WriteLine($"FAIL  {test.Name}");
        Console.WriteLine(exception);
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine($"{failures.Count} self-test(s) failed.");
    return 1;
}

Console.WriteLine($"All {tests.Count} self-tests passed.");
return 0;

static IEosCueImporterFactory CreateFactory() => new EosCueImporterFactory(
    new IEosCueImporter[] { new CsvEosCueImporter(), new Esf3dEosCueImporter() });

static Task TestFactoryAsync()
{
    var factory = CreateFactory();
    Assert(factory.CreateFor("show.csv").SourceKind == EosSourceKind.Csv, "CSV strategy was not selected.");
    Assert(factory.CreateFor("show.esf3d").SourceKind == EosSourceKind.Esf3d, "ESF3D strategy was not selected.");
    return Task.CompletedTask;
}

static async Task TestCsvAsync()
{
    var path = Path.Combine(AppContext.BaseDirectory, "samples", "reference-eos.csv");
    await using var stream = File.OpenRead(path);
    var result = await CreateFactory().CreateFor(path)
        .ImportAsync(new EosImportRequest(Path.GetFileName(path), stream));

    Assert(result.Cues.Count == 4, "Cue-part aggregation did not produce the expected cue count.");
    Assert(result.Cues.Any(cue => cue.CueNumber == "4" && cue.SceneText == "Szene 1"), "SCENE_TEXT was not mapped.");
    Assert(result.Cues.Any(cue => cue.CueNumber == "1" && cue.Label == "Blackout"), "LABEL was not mapped.");
    Assert(result.Cues.Any(cue => cue.CueNumber == "2" && cue.Follow == "F3"), "FOLLOW was not mapped by column name.");
    Assert(result.Diagnostics.OfType<CsvCuePartsMergedWarning>().Any(), "Cue part aggregation was not reported.");
}

static async Task TestEsf3dAsync()
{
    var path = Path.Combine(AppContext.BaseDirectory, "samples", "synthetic-parser-fixture.esf3d");
    await using var stream = File.OpenRead(path);
    var result = await CreateFactory().CreateFor(path)
        .ImportAsync(new EosImportRequest(Path.GetFileName(path), stream));

    Assert(result.SourceKind == EosSourceKind.Esf3d, "The source kind is incorrect.");
    Assert(result.Cues.Count > 0, "The synthetic ESF3D fixture returned no cues.");
    Assert(result.Diagnostics.OfType<Esf3dLossTolerantParsingWarning>().Any(), "The loss-tolerant parser warning is missing.");
}

static Task TestFollowLogicAsync()
{
    var cues = new[]
    {
        Cue(0, "1", follow: "F"),
        Cue(1, "2"),
        Cue(2, "3")
    };
    var diagnostics = new List<EosDiagnostic>();
    var plan = new QLabImportPlanBuilder().Build(cues, Options(), diagnostics);
    var networkCues = plan.Items.OfType<QLabNetworkCuePlan>().ToArray();

    Assert(networkCues.Length == 2, "Follow/hang did not skip exactly one cue.");
    Assert(networkCues[0].CueNumber == "1" && networkCues[1].CueNumber == "3", "The wrong cue was skipped.");
    Assert(diagnostics.OfType<CueSkippedAfterFollowOrHangWarning>().Count() == 1, "The skip warning count is incorrect.");
    return Task.CompletedTask;
}

static Task TestSceneTextAsync()
{
    var cues = new[]
    {
        Cue(0, "1", scene: "Scene A"),
        Cue(1, "2", scene: "Scene A"),
        Cue(2, "3", scene: "Scene B")
    };
    var plan = new QLabImportPlanBuilder().Build(cues, Options());
    var memos = plan.Items.OfType<QLabMemoCuePlan>().ToArray();
    Assert(memos.Length == 2, "Repeated scene text created duplicate memo cues.");
    Assert(memos[0].Name == "Scene A" && memos[1].Name == "Scene B", "Scene memo order is incorrect.");
    return Task.CompletedTask;
}

static EosCue Cue(int order, string number, string? follow = null, string? scene = null) => new()
{
    SourceOrder = order,
    ListNumber = 1,
    CueNumber = number,
    Follow = follow,
    SceneText = scene,
    SourceKind = EosSourceKind.Csv
};

static QLabImportOptions Options() => new()
{
    WorkspaceId = "test",
    CueListName = "Test",
    SceneTextMode = SceneTextImportMode.MemoCueAndNotes,
    NetworkPatchName = "Eos"
};

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
