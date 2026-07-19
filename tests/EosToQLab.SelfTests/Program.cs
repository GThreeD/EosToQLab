using EosToQLab.Core.Diagnostics;
using EosToQLab.Core.Exceptions;
using EosToQLab.Core.Import;
using EosToQLab.Core.Models;
using EosToQLab.Core.Planning;
using EosToQLab.Infrastructure.Import;
using EosToQLab.Infrastructure.Import.Csv;
using EosToQLab.Infrastructure.Import.Esf3d;
using EosToQLab.Infrastructure.QLab;
using EosToQLab.Infrastructure.QLab.Workflow;
using EosToQLab.Infrastructure.QLab.Osc;

var tests = new List<(string Name, Func<Task> Run)>
{
    ("Factory selects CSV and ESF3D strategies", TestFactoryAsync),
    ("Reference EOS CSV maps into the common cue model", TestCsvAsync),
    ("Synthetic ESF3D maps into the common cue model", TestEsf3dAsync),
    ("Follow/hang chains can be excluded", TestFollowExcludeChainAsync),
    ("Follow/hang chains can be imported disarmed", TestFollowImportDisarmedAsync),
    ("Follow/hang state is isolated per EOS list", TestFollowStatePerListAsync),
    ("Manual cue selection preserves follow/hang classification", TestManualSelectionPreservesFollowChainAsync),
    ("Scene text creates memo cues without duplication", TestSceneTextAsync),
    ("Plans always request the exact EOS cue number", TestDesiredNumberingAsync),
    ("Duplicate EOS cue numbers are attempted independently", TestDuplicateDesiredNumberingAsync),
    ("Memo plan maps to declarative QLab properties", TestMemoMapperAsync),
    ("Network plan maps the EOS parameter stack", TestNetworkMapperAsync),
    ("EOS user selection shifts visible network parameters", TestEosUserParameterStackAsync),
    ("EOS Run cue omits the list parameter", TestEosRunCueParameterStackAsync),
    ("Memo creation leaves QLab numbering untouched", TestMemoCreationDoesNotTouchNumberAsync),
    ("Network parameters are sent as ordered individual writes", TestIndividualNetworkParametersAndSinglePatchVerificationAsync),
    ("Buffered SLIP reader preserves consecutive frames", TestBufferedSlipReaderAsync),
    ("Rejected QLab numbers leave cues unnumbered", TestRejectedNumberLeavesBlankAsync),
    ("Available QLab numbers use the exact EOS number", TestSuccessfulNumberAssignmentAsync)
};

var failures = new List<string>();
foreach (var test in tests)
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

if (failures.Count > 0)
{
    Console.Error.WriteLine($"{failures.Count} self-test(s) failed.");
    return 1;
}

Console.WriteLine($"All {tests.Count} self-tests passed.");
return 0;

static IEosCueImporterFactory CreateFactory()
{
    return new EosCueImporterFactory(
        new IEosCueImporter[] { new CsvEosCueImporter(), new Esf3dEosCueImporter() });
}

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
    Assert(result.Cues.Count == 4, "The synthetic ESF3D fixture returned the wrong cue count.");

    var first = result.Cues.Single(cue => cue.CueNumber == "1");
    Assert(first.Label == "Blackout", "The ESF3D cue label was not mapped.");
    Assert(first.CueNotes == "Network note", "The ESF3D cue notes were not mapped.");
    Assert(first.SceneText == "Scene A", "The ESF3D scene text was not mapped.");
    Assert(first.Follow == "F3", "The ESF3D follow value was not mapped.");

    var second = result.Cues.Single(cue => cue.CueNumber == "2");
    Assert(second.Follow == "H1.5", "The structured ESF3D hang value was not mapped.");

    var fourth = result.Cues.Single(cue => cue.CueNumber == "4");
    Assert(fourth.Follow == "F1", "The current EOS continuation-object follow value was not mapped.");
    Assert(!result.Diagnostics.OfType<Esf3dFollowNotDecodedWarning>().Any(), "A decoded follow/hang value was reported as unsupported.");
    Assert(result.Diagnostics.OfType<Esf3dLossTolerantParsingWarning>().Any(), "The loss-tolerant parser warning is missing.");
}

static Task TestFollowExcludeChainAsync()
{
    var cues = new[]
    {
        Cue(0, "83.1", follow: "F1"),
        Cue(1, "83.2", follow: "F1"),
        Cue(2, "83.3", follow: "F1"),
        Cue(3, "84"),
        Cue(4, "85")
    };
    var diagnostics = new List<EosDiagnostic>();
    var plan = new QLabImportPlanBuilder().Build(
        cues,
        Options() with { FollowedCueMode = FollowedCueImportMode.Exclude },
        diagnostics);
    var networkCues = plan.Items.OfType<QLabNetworkCuePlan>().ToArray();

    Assert(networkCues.Select(cue => cue.CueNumber).SequenceEqual(["83.1", "85"]),
        "The complete follow chain was not excluded.");
    Assert(diagnostics.OfType<CueSkippedAfterFollowOrHangWarning>().Count() == 3,
        "Every automatically triggered cue should produce an exclusion warning.");
    return Task.CompletedTask;
}

static Task TestFollowImportDisarmedAsync()
{
    var cues = new[]
    {
        Cue(0, "83.1", follow: "F1"),
        Cue(1, "83.2", follow: "F1"),
        Cue(2, "83.3", follow: "F1"),
        Cue(3, "84"),
        Cue(4, "85")
    };
    var diagnostics = new List<EosDiagnostic>();
    var plan = new QLabImportPlanBuilder().Build(
        cues,
        Options() with { FollowedCueMode = FollowedCueImportMode.ImportDisarmed },
        diagnostics);
    var networkCues = plan.Items.OfType<QLabNetworkCuePlan>().ToArray();

    Assert(networkCues.Length == 5, "Disarmed mode must keep every cue in the plan.");
    Assert(networkCues.Where(cue => cue.CueNumber is "83.2" or "83.3" or "84").All(cue => !cue.Armed),
        "Automatically triggered cues were not disarmed.");
    Assert(networkCues.Where(cue => cue.CueNumber is "83.1" or "85").All(cue => cue.Armed),
        "Manually triggered cues must stay armed.");
    Assert(diagnostics.OfType<CueImportedDisarmedAfterFollowOrHangWarning>().Count() == 3,
        "Every automatically triggered cue should produce a disarmed warning.");
    return Task.CompletedTask;
}

static Task TestFollowStatePerListAsync()
{
    var cues = new[]
    {
        Cue(0, "1", follow: "F1", listNumber: 1),
        Cue(1, "1", listNumber: 2),
        Cue(2, "2", listNumber: 1)
    };
    var plan = new QLabImportPlanBuilder().Build(
        cues,
        Options() with { FollowedCueMode = FollowedCueImportMode.Exclude });
    var networkCues = plan.Items.OfType<QLabNetworkCuePlan>().ToArray();

    Assert(networkCues.Any(cue => cue.ListNumber == "2" && cue.CueNumber == "1"),
        "A Follow in EOS list 1 must not suppress a cue in EOS list 2.");
    Assert(networkCues.All(cue => !(cue.ListNumber == "1" && cue.CueNumber == "2")),
        "The next cue in EOS list 1 should be excluded.");
    Assert(networkCues.Length == 2, "Follow/hang did not skip exactly one cue.");
    Assert(networkCues[0].CueNumber == "1" && networkCues[1].CueNumber == "3", "The wrong cue was skipped.");
    return Task.CompletedTask;
}

static Task TestManualSelectionPreservesFollowChainAsync()
{
    var cues = new[]
    {
        Cue(0, "83.1", follow: "F1") with { ImportEnabled = false },
        Cue(1, "83.2"),
        Cue(2, "84")
    };
    var plan = new QLabImportPlanBuilder().Build(
        cues,
        Options() with { FollowedCueMode = FollowedCueImportMode.ImportDisarmed });
    var networkCues = plan.Items.OfType<QLabNetworkCuePlan>().ToArray();

    Assert(networkCues.Length == 2, "The manually deselected cue should not be imported.");
    Assert(networkCues[0].CueNumber == "83.2" && !networkCues[0].Armed,
        "A cue following a manually deselected Follow cue must remain classified as automatic.");
    Assert(networkCues[1].CueNumber == "84" && networkCues[1].Armed,
        "The follow chain should end after the first cue without Follow/Hang.");
    return Task.CompletedTask;
}

static Task TestSceneTextAsync()
{
    var cues = new[]
    {
        Cue(0, "1", "Network A", "Note A", scene: "Scene A"),
        Cue(1, "2", "Network B", scene: "Scene A"),
        Cue(2, "3", "Network C", "Note C", scene: "Scene B")
    };
    var plan = new QLabImportPlanBuilder().Build(cues, Options());
    var memos = plan.Items.OfType<QLabMemoCuePlan>().ToArray();
    var networkCues = plan.Items.OfType<QLabNetworkCuePlan>().ToArray();

    Assert(memos.Length == 2, "Repeated scene text created duplicate memo cues.");
    Assert(memos[0].Name == "Scene A" && memos[1].Name == "Scene B", "Scene memo order is incorrect.");
    Assert(memos.All(memo => memo.Notes is null), "Scene memos must not receive generated notes.");
    Assert(networkCues.Select(cue => cue.Name).SequenceEqual(["Network A", "Network B", "Network C"]),
        "Network cue names must contain only the EOS label.");
    Assert(networkCues[0].Notes == "Note A" && networkCues[1].Notes is null && networkCues[2].Notes == "Note C",
        "Network cue notes must contain only EOS cue notes.");
    Assert(networkCues.Select(cue => cue.QLabNumber).SequenceEqual(["1", "2", "3"]),
        "Every network cue must retain its EOS cue number as the desired QLab number.");
    return Task.CompletedTask;
}

static Task TestDesiredNumberingAsync()
{
    var cues = new[]
    {
        Cue(0, "1", listNumber: 1),
        Cue(1, "2.5", listNumber: 2)
    };
    var plan = new QLabImportPlanBuilder().Build(cues, Options());
    var networkCues = plan.Items.OfType<QLabNetworkCuePlan>().ToArray();

    Assert(networkCues.Select(cue => cue.QLabNumber).SequenceEqual(["1", "2.5"]),
        "QLab numbers must always be requested exactly as they appear in EOS.");
    return Task.CompletedTask;
}

static Task TestDuplicateDesiredNumberingAsync()
{
    var cues = new[]
    {
        Cue(0, "1", listNumber: 1),
        Cue(1, "1", listNumber: 2),
        Cue(2, "2", listNumber: 2)
    };
    var plan = new QLabImportPlanBuilder().Build(cues, Options());
    var networkCues = plan.Items.OfType<QLabNetworkCuePlan>().ToArray();

    Assert(networkCues.Select(cue => cue.QLabNumber).SequenceEqual(["1", "1", "2"]),
        "Duplicate EOS cue numbers must still be attempted individually in QLab.");
    Assert(networkCues[1].ListNumber == "2" && networkCues[1].CueNumber == "1",
        "QLab number handling must not change the EOS network command parameters.");
    return Task.CompletedTask;
}

static Task TestMemoMapperAsync()
{
    var request = new QLabMemoCuePlanMapper().Map(
        new QLabMemoCuePlan("Scene A", "From EOS"),
        new QLabPlanExecutionContext(new QLabNetworkPatch("patch", "EOS", "eos")));

    Assert(request.CueType == QLabCueType.Memo, "Memo cue type was not mapped.");
    Assert(request.CueProperties.Any(property =>
        property.Property == QLabCueProperty.Name
        && Equals(property.Value, "Scene A")), "Memo name property is missing.");
    Assert(request.CueProperties.All(property =>
        property.Property != QLabCueProperty.Number), "Memo mapping must not set an empty QLab number.");
    Assert(request.CueProperties.Any(property =>
        property.Property == QLabCueProperty.Armed
        && Equals(property.Value, false)), "Memo cue should be disarmed.");
    Assert(request.CueProperties.Any(property =>
        property.Property == QLabCueProperty.SkipIfDisarmed
        && Equals(property.Value, true)), "Memo cue should be skipped when disarmed.");
    Assert(request.NetworkParameters.Count == 0, "Memo mapping created network parameters.");
    return Task.CompletedTask;
}

static Task TestNetworkMapperAsync()
{
    var patch = new QLabNetworkPatch("patch-id", "EOS", "eos");
    var request = new QLabNetworkCuePlanMapper().Map(
        new QLabNetworkCuePlan("LX 1/2", "1", "2", "2", "Notes", Armed: false),
        new QLabPlanExecutionContext(patch));

    Assert(request.CueType == QLabCueType.Network, "Network cue type was not mapped.");
    Assert(request.ExpectedNetworkPatch == patch, "Expected network patch is missing.");
    Assert(request.CueProperties.Any(property =>
        property.Property == QLabCueProperty.NetworkPatchId
        && Equals(property.Value, patch.Id)), "Network patch property is missing.");
    Assert(request.CueProperties.All(property =>
            property.Property != QLabCueProperty.Number),
        "The mapper must defer QLab number assignment until all cues are unnumbered.");
    Assert(request.CueProperties.Any(property =>
        property.Property == QLabCueProperty.Armed
        && Equals(property.Value, false)),
        "A followed EOS cue must be mapped to an explicitly disarmed QLab cue.");
    Assert(request.DesiredCueNumber == "2",
        "The desired QLab number must equal the EOS cue number exactly.");
    Assert(request.NetworkParameters.Select(parameter => (parameter.Index, parameter.Parameter, parameter.Value))
        .SequenceEqual(new[]
        {
            (0, QLabEosParameter.Type, "Cues"),
            (1, QLabEosParameter.SpecifyUser, "No"),
            (2, QLabEosParameter.Command, "Run cue in specific list"),
            (3, QLabEosParameter.List, "1"),
            (4, QLabEosParameter.Cue, "2")
        }), "The EOS parameter stack is incorrect.");
    return Task.CompletedTask;
}

static Task TestEosUserParameterStackAsync()
{
    var command = QLabEosNetworkCommand.RunCueInSpecificList("7", "12.5", user: "3");
    var parameters = command.BuildParameters();

    Assert(parameters.Select(parameter => (parameter.Index, parameter.Parameter, parameter.Value))
        .SequenceEqual(new[]
        {
            (0, QLabEosParameter.Type, "Cues"),
            (1, QLabEosParameter.SpecifyUser, "Yes"),
            (2, QLabEosParameter.User, "3"),
            (3, QLabEosParameter.Command, "Run cue in specific list"),
            (4, QLabEosParameter.List, "7"),
            (5, QLabEosParameter.Cue, "12.5")
        }), "Specify user did not shift the visible parameter indices.");
    return Task.CompletedTask;
}

static Task TestEosRunCueParameterStackAsync()
{
    var command = QLabEosNetworkCommand.RunCue("12.5");
    var parameters = command.BuildParameters();

    Assert(parameters.Select(parameter => (parameter.Index, parameter.Parameter, parameter.Value))
        .SequenceEqual(new[]
        {
            (0, QLabEosParameter.Type, "Cues"),
            (1, QLabEosParameter.SpecifyUser, "No"),
            (2, QLabEosParameter.Command, "Run cue"),
            (3, QLabEosParameter.Cue, "12.5")
        }), "Run cue must not emit a List parameter.");
    return Task.CompletedTask;
}

static async Task TestMemoCreationDoesNotTouchNumberAsync()
{
    var patch = new QLabNetworkPatch("patch-id", "EOS", "eos");
    await using var session = new TrackingNumberSession(patch.Id);
    var executor = new QLabImportPlanExecutor([new QLabMemoCuePlanMapper()]);
    var plan = new QLabImportPlan(
    [
        new QLabMemoCuePlan("Scene A", null)
    ]);

    await executor.ExecuteAsync(
        session,
        plan,
        new QLabPlanExecutionContext(patch));

    Assert(session.NumberWrites.Count == 0,
        "Memo creation must not clear or assign a QLab cue number.");
}

static async Task TestIndividualNetworkParametersAndSinglePatchVerificationAsync()
{
    var patch = new QLabNetworkPatch("patch-id", "EOS", "eos");
    await using var session = new TrackingNumberSession(patch.Id);
    var executor = new QLabImportPlanExecutor([new QLabNetworkCuePlanMapper()]);
    var plan = new QLabImportPlan(
    [
        new QLabNetworkCuePlan("LX 1", "1", "1", "1", null),
        new QLabNetworkCuePlan("LX 2", "1", "2", "2", null)
    ]);

    await executor.ExecuteAsync(
        session,
        plan,
        new QLabPlanExecutionContext(patch));

    Assert(session.NetworkParameterWrites.Count == 10,
        "Each network cue should receive its five EOS parameters.");
    Assert(session.NetworkParameterWrites.Take(5).SequenceEqual(
        new[]
        {
            (0, "Cues"),
            (1, "No"),
            (2, "Run cue in specific list"),
            (3, "1"),
            (4, "1")
        }),
        "The first network cue parameter sequence is incorrect.");
    Assert(session.NetworkPatchQueryCount == 1,
        "A shared network patch should be verified only once per import.");
}

static async Task TestBufferedSlipReaderAsync()
{
    byte[] firstPayload = [1, SlipCodec.End, 2, SlipCodec.Escape, 3];
    byte[] secondPayload = [4, 5, 6];
    var firstFrame = SlipCodec.Frame(firstPayload);
    var secondFrame = SlipCodec.Frame(secondPayload);
    var streamBytes = firstFrame.Concat(secondFrame).ToArray();

    await using var stream = new MemoryStream(streamBytes);
    var reader = new SlipStreamReader(stream, bufferSize: 5);

    var decodedFirst = await reader.ReadFrameAsync(CancellationToken.None);
    var decodedSecond = await reader.ReadFrameAsync(CancellationToken.None);

    Assert(decodedFirst.SequenceEqual(firstPayload),
        "The buffered SLIP reader corrupted an escaped payload.");
    Assert(decodedSecond.SequenceEqual(secondPayload),
        "The buffered SLIP reader lost bytes from the following frame.");
}

static async Task TestRejectedNumberLeavesBlankAsync()
{
    var patch = new QLabNetworkPatch("patch-id", "EOS", "eos");
    await using var session = new TrackingNumberSession(patch.Id, ["1"]);
    var executor = new QLabImportPlanExecutor([new QLabNetworkCuePlanMapper()]);
    var plan = new QLabImportPlan(
    [
        new QLabNetworkCuePlan("LX 1", "1", "1", "1", null)
    ]);

    var result = await executor.ExecuteAsync(
        session,
        plan,
        new QLabPlanExecutionContext(patch));
    var assignedCount = await QLabImportPlanExecutor.AssignCueNumbersAsync(
        session,
        result.PendingCueNumbers);

    Assert(session.NumberWrites.SequenceEqual(["", "1"]),
        "The cue must be cleared first and then receive exactly one EOS-number attempt.");
    Assert(assignedCount == 0,
        "A rejected QLab number must not be counted as assigned.");
    Assert(session.CurrentNumber == string.Empty,
        "A conflicting EOS cue number must leave the QLab cue unnumbered.");
    Assert(!session.NumberWrites.Any(value => value == "2"),
        "The importer must never increment a conflicting QLab cue number.");
    Assert(session.NetworkParameterWrites.SequenceEqual(
        new[]
        {
            (0, "Cues"),
            (1, "No"),
            (2, "Run cue in specific list"),
            (3, "1"),
            (4, "1")
        }),
        "The ordered network parameter values are incorrect.");
}

static async Task TestSuccessfulNumberAssignmentAsync()
{
    var patch = new QLabNetworkPatch("patch-id", "EOS", "eos");
    await using var session = new TrackingNumberSession(patch.Id);
    var executor = new QLabImportPlanExecutor([new QLabNetworkCuePlanMapper()]);
    var plan = new QLabImportPlan(
    [
        new QLabNetworkCuePlan("LX 7.5", "2", "7.5", "7.5", null)
    ]);

    var result = await executor.ExecuteAsync(
        session,
        plan,
        new QLabPlanExecutionContext(patch));
    var assignedCount = await QLabImportPlanExecutor.AssignCueNumbersAsync(
        session,
        result.PendingCueNumbers);

    Assert(session.NumberWrites.SequenceEqual(["", "7.5"]),
        "QLab must receive only the exact EOS cue number after the initial clear.");
    Assert(session.CurrentNumber == "7.5",
        "An available QLab cue number must equal the EOS cue number.");
    Assert(assignedCount == 1,
        "A successful exact EOS number must be counted for rollback.");
}

static EosCue Cue(
    int order,
    string number,
    string? label = null,
    string? notes = null,
    string? follow = null,
    string? scene = null,
    int listNumber = 1)
{
    return new EosCue
    {
        SourceOrder = order,
        ListNumber = listNumber,
        CueNumber = number,
        Label = label,
        CueNotes = notes,
        Follow = follow,
        SceneText = scene,
        SourceKind = EosSourceKind.Csv
    };
}

static QLabImportOptions Options()
{
    return new QLabImportOptions
    {
        WorkspaceId = "test",
        CueListName = "Test",
        SceneTextMode = SceneTextImportMode.MemoCue,
        NetworkPatchId = "patch-id",
        NetworkPatchName = "Eos"
    };
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

internal sealed class TrackingNumberSession : IQLabOscSession
{
    private readonly string _patchId;
    private readonly HashSet<string> _rejectedNumbers;

    public TrackingNumberSession(string patchId, IEnumerable<string>? rejectedNumbers = null)
    {
        _patchId = patchId;
        _rejectedNumbers = rejectedNumbers?.ToHashSet(StringComparer.OrdinalIgnoreCase)
                           ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public QLabWorkspace Workspace { get; } = new("workspace", "Test", null);
    public List<QLabCueProperty> SetProperties { get; } = [];
    public List<(int Index, string Value)> NetworkParameterWrites { get; } = [];
    public List<string> NumberWrites { get; } = [];
    public int NetworkPatchQueryCount { get; private set; }
    public string CurrentNumber { get; private set; } = "auto-1";

    public Task<string> CreateCueAsync(
        QLabCueType cueType,
        string cueName,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult("cue-id");
    }

    public Task SetCuePropertyAsync(
        string cueId,
        QLabCueProperty property,
        object? value,
        CancellationToken cancellationToken = default)
    {
        if (property == QLabCueProperty.Number)
        {
            var number = value?.ToString() ?? string.Empty;
            NumberWrites.Add(number);
            if (number.Length > 0 && _rejectedNumbers.Contains(number))
                throw new QLabUnexpectedReplyException(
                    "/reply/workspace/workspace/cue_id/cue-id/number",
                    "{\"status\":\"error\"}");

            CurrentNumber = number;
            return Task.CompletedTask;
        }

        SetProperties.Add(property);
        return Task.CompletedTask;
    }

    public Task SetNetworkParameterAsync(
        string cueId,
        int parameterIndex,
        string value,
        CancellationToken cancellationToken = default)
    {
        NetworkParameterWrites.Add((parameterIndex, value));
        return Task.CompletedTask;
    }

    public Task<string?> QueryCuePropertyAsync(
        string cueId,
        QLabCueProperty property,
        CancellationToken cancellationToken = default)
    {
        if (property == QLabCueProperty.NetworkPatchId)
        {
            NetworkPatchQueryCount++;
            return Task.FromResult<string?>(_patchId);
        }

        return Task.FromResult<string?>(property == QLabCueProperty.Number
            ? CurrentNumber
            : null);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public Task<IReadOnlyList<QLabCueList>> GetCueListsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<IReadOnlyList<QLabNetworkPatch>> GetNetworkPatchesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<QLabNetworkPatch>>(
            [new QLabNetworkPatch(_patchId, "EOS", "eos")]);
    }

    public Task<string?> GetCurrentCueListIdAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task SetWorkspacePropertyAsync(QLabWorkspaceProperty property, object? value,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task RenameCueListAsync(string cueListId, string currentName, string targetName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task DeleteCueListAsync(string cueListId, string cueListName, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task SaveWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task UndoAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}