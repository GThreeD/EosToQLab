using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Planning;

public sealed class QLabImportPlanBuilderTests
{
    private readonly QLabImportPlanBuilder _sut = new();

    [Fact]
    public void Rejects_null_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.Build(null!, TestData.Options()));
        Assert.Throws<ArgumentNullException>(() => _sut.Build([], null!));
    }

    [Fact]
    public void Orders_by_source_order_trims_values_and_creates_scene_memos()
    {
        var cues = new[]
        {
            TestData.Cue(2, "3", " C ", " Note C ", scene: " Scene B "),
            TestData.Cue(0, "1", " A ", " Note A ", scene: " Scene A "),
            TestData.Cue(1, "2", null, " ", scene: "Scene A")
        };

        var plan = _sut.Build(cues, TestData.Options());

        Assert.Collection(plan.Items,
            item => Assert.Equal("Scene A", Assert.IsType<QLabMemoCuePlan>(item).Name),
            item => Assert.Equal(("A", "Note A", "1"), Network(item)),
            item => Assert.Equal("Scene A", Assert.IsType<QLabMemoCuePlan>(item).Name),
            item => Assert.Equal((string.Empty, null, "2"), Network(item)),
            item => Assert.Equal("Scene B", Assert.IsType<QLabMemoCuePlan>(item).Name),
            item => Assert.Equal(("C", "Note C", "3"), Network(item)));
    }

    [Fact]
    public void Ignore_scene_mode_does_not_create_memos()
    {
        var plan = _sut.Build([TestData.Cue(scene: "Scene")],
            TestData.Options() with { SceneTextMode = SceneTextImportMode.Ignore });
        Assert.Empty(plan.Items.OfType<QLabMemoCuePlan>());
    }

    [Fact]
    public void Exclude_mode_removes_complete_follow_chain_and_reports_each_skipped_cue()
    {
        var cues = new[]
        {
            TestData.Cue(0, "83.1", follow: "F1"),
            TestData.Cue(1, "83.2", follow: "F1"),
            TestData.Cue(2, "83.3", follow: "H1"),
            TestData.Cue(3, "84"),
            TestData.Cue(4, "85")
        };
        List<EosDiagnostic> diagnostics = [];

        var plan = _sut.Build(cues, TestData.Options(), diagnostics);

        Assert.Equal(["83.1", "85"], plan.Items.OfType<QLabNetworkCuePlan>().Select(x => x.CueNumber));
        Assert.Equal(3, diagnostics.OfType<CueSkippedAfterFollowOrHangWarning>().Count());
    }

    [Fact]
    public void Import_disarmed_mode_keeps_followed_cues_disarmed()
    {
        List<EosDiagnostic> diagnostics = [];
        var plan = _sut.Build([
            TestData.Cue(follow: "F1"),
            TestData.Cue(1, "2", follow: "F1"),
            TestData.Cue(2, "3"),
            TestData.Cue(3, "4")
        ], TestData.Options() with { FollowedCueMode = FollowedCueImportMode.ImportDisarmed }, diagnostics);

        var network = plan.Items.OfType<QLabNetworkCuePlan>().ToArray();
        Assert.True(network[0].Armed);
        Assert.False(network[1].Armed);
        Assert.False(network[2].Armed);
        Assert.True(network[3].Armed);
        Assert.Equal(2, diagnostics.OfType<CueImportedDisarmedAfterFollowOrHangWarning>().Count());
    }

    [Fact]
    public void Follow_state_is_isolated_per_list_and_includes_deselected_cues()
    {
        var plan = _sut.Build([
            TestData.Cue(follow: "F1", listNumber: 1, importEnabled: false),
            TestData.Cue(1, listNumber: 2),
            TestData.Cue(2, "2", listNumber: 1)
        ], TestData.Options() with { FollowedCueMode = FollowedCueImportMode.ImportDisarmed });

        var network = plan.Items.OfType<QLabNetworkCuePlan>().ToArray();
        Assert.Equal(2, network.Length);
        Assert.True(network.Single(x => x.ListNumber == "2").Armed);
        Assert.False(network.Single(x => x.ListNumber == "1").Armed);
    }

    [Fact]
    public void Diagnostics_collection_is_optional()
    {
        var plan = _sut.Build([TestData.Cue(follow: "F1"), TestData.Cue(1, "2")], TestData.Options());
        Assert.Single(plan.Items.OfType<QLabNetworkCuePlan>());
    }

    private static (string Name, string? Notes, string Number) Network(QLabPlanItem item)
    {
        var network = Assert.IsType<QLabNetworkCuePlan>(item);
        Assert.Equal(network.CueNumber, network.QLabNumber);
        return (network.Name, network.Notes, network.CueNumber);
    }
}