namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabMemoCuePlanMapperTests
{
    [Fact]
    public void Maps_memo_properties_and_optional_notes()
    {
        var context = new QLabPlanExecutionContext(new QLabNetworkPatch("id", "Eos", "eos"));
        var request = new QLabMemoCuePlanMapper().Map(new QLabMemoCuePlan("Scene", "note", true, false), context);
        Assert.Equal(QLabCueType.Memo, request.CueType);
        Assert.Contains(new QLabCuePropertyAssignment(QLabCueProperty.Name, "Scene"), request.CueProperties);
        Assert.Contains(new QLabCuePropertyAssignment(QLabCueProperty.Notes, "note"), request.CueProperties);
        Assert.Contains(new QLabCuePropertyAssignment(QLabCueProperty.Armed, true), request.CueProperties);
        Assert.Contains(new QLabCuePropertyAssignment(QLabCueProperty.SkipIfDisarmed, false), request.CueProperties);
        Assert.Empty(request.NetworkParameters);
        Assert.Null(request.DesiredCueNumber);
    }

    [Fact]
    public void Omits_blank_notes()
    {
        var request = new QLabMemoCuePlanMapper().Map(
            new QLabMemoCuePlan("Scene", " "),
            new QLabPlanExecutionContext(new QLabNetworkPatch("id", "Eos", "eos")));
        Assert.DoesNotContain(request.CueProperties, x => x.Property == QLabCueProperty.Notes);
    }
}