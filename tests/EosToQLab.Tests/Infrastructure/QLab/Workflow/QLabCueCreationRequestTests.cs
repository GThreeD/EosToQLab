namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabCueCreationRequestTests
{
    [Fact]
    public void Stores_request_values_and_defaults()
    {
        var property = new QLabCuePropertyAssignment(QLabCueProperty.Name, "Cue");
        var parameter = new QLabNetworkParameterAssignment(0, QLabEosParameter.Type, "Cues");
        var patch = new QLabNetworkPatch("id", "EOS", "eos");

        var request = new QLabCueCreationRequest(
            QLabCueType.Network,
            "Cue",
            [property],
            [parameter],
            patch,
            "1");

        Assert.Equal(QLabCueType.Network, request.CueType);
        Assert.Equal("Cue", request.Name);
        Assert.Equal([property], request.CueProperties);
        Assert.Equal([parameter], request.NetworkParameters);
        Assert.Equal(patch, request.ExpectedNetworkPatch);
        Assert.Equal("1", request.DesiredCueNumber);
    }

    [Fact]
    public void Optional_values_default_to_null()
    {
        var request = new QLabCueCreationRequest(
            QLabCueType.Memo,
            "Memo",
            [],
            []);

        Assert.Null(request.ExpectedNetworkPatch);
        Assert.Null(request.DesiredCueNumber);
    }
}