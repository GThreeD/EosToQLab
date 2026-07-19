namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabNetworkCuePlanMapperTests
{
    [Fact]
    public void Maps_patch_notes_disarmed_state_number_and_Eos_parameters()
    {
        var patch = new QLabNetworkPatch("patch", "Eos", "eos");
        var request = new QLabNetworkCuePlanMapper().Map(
            new QLabNetworkCuePlan("Cue", "2", "7.5", "7.5", "note", false),
            new QLabPlanExecutionContext(patch));

        Assert.Equal(QLabCueType.Network, request.CueType);
        Assert.Equal(patch, request.ExpectedNetworkPatch);
        Assert.Equal("7.5", request.DesiredCueNumber);
        Assert.Contains(new QLabCuePropertyAssignment(QLabCueProperty.NetworkPatchId, "patch"), request.CueProperties);
        Assert.Contains(new QLabCuePropertyAssignment(QLabCueProperty.Notes, "note"), request.CueProperties);
        Assert.Contains(new QLabCuePropertyAssignment(QLabCueProperty.Armed, false), request.CueProperties);
        Assert.Equal(["Cues", "No", "Run cue in specific list", "2", "7.5"],
            request.NetworkParameters.Select(x => x.Value));
    }

    [Fact]
    public void Armed_cue_with_blank_notes_omits_optional_properties()
    {
        var request = new QLabNetworkCuePlanMapper().Map(
            new QLabNetworkCuePlan("Cue", "1", "1", "1", " "),
            new QLabPlanExecutionContext(new QLabNetworkPatch("patch", "Eos", "eos")));
        Assert.DoesNotContain(request.CueProperties, x => x.Property is QLabCueProperty.Notes or QLabCueProperty.Armed);
    }
}