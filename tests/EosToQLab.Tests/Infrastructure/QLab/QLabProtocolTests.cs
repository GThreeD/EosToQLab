namespace EosToQLab.Tests.Infrastructure.QLab;

public sealed class QLabProtocolTests
{
    [Fact]
    public void Builds_addresses_commands_and_protocol_names()
    {
        Assert.Equal("/workspace/w/save", QLabProtocol.Addresses.Workspace("w", "save"));
        Assert.Equal("/workspace/w/cue_id/c/name", QLabProtocol.Addresses.Cue("w", "c", "name"));
        Assert.Equal("delete_id/c", QLabProtocol.WorkspaceCommands.DeleteById("c"));
        Assert.Equal("cue list", QLabProtocol.CueTypeName(QLabCueType.CueList));
        Assert.Equal("memo", QLabProtocol.CueTypeName(QLabCueType.Memo));
        Assert.Equal("network", QLabProtocol.CueTypeName(QLabCueType.Network));
        Assert.Equal("name", QLabProtocol.CuePropertyName(QLabCueProperty.Name));
        Assert.Equal("number", QLabProtocol.CuePropertyName(QLabCueProperty.Number));
        Assert.Equal("notes", QLabProtocol.CuePropertyName(QLabCueProperty.Notes));
        Assert.Equal("armed", QLabProtocol.CuePropertyName(QLabCueProperty.Armed));
        Assert.Equal("skipIfDisarmed", QLabProtocol.CuePropertyName(QLabCueProperty.SkipIfDisarmed));
        Assert.Equal("networkPatchID", QLabProtocol.CuePropertyName(QLabCueProperty.NetworkPatchId));
        Assert.Equal("currentCueListID", QLabProtocol.WorkspacePropertyName(QLabWorkspaceProperty.CurrentCueListId));
        Assert.Equal("parameterValue/3", QLabProtocol.NetworkParameterProperty(3));
    }

    [Fact]
    public void Rejects_unknown_enum_values_and_negative_parameter_index()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => QLabProtocol.CueTypeName((QLabCueType)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => QLabProtocol.CuePropertyName((QLabCueProperty)999));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            QLabProtocol.WorkspacePropertyName((QLabWorkspaceProperty)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => QLabProtocol.NetworkParameterProperty(-1));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("ETC Eos Family", true)]
    [InlineData("EOS", true)]
    [InlineData("Generic OSC", false)]
    public void Identifies_Eos_patch_types(string? type, bool expected)
    {
        Assert.Equal(expected, QLabProtocol.IsEosNetworkPatchType(type));
    }
}