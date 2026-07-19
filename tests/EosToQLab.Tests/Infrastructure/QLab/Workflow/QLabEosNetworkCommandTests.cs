namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabEosNetworkCommandTests
{
    [Fact]
    public void RunCueInSpecificList_builds_visible_stack_without_user()
    {
        var parameters = QLabEosNetworkCommand.RunCueInSpecificList(" 7 ", " 12.5 ").BuildParameters();
        Assert.Equal([
            new QLabNetworkParameterAssignment(0, QLabEosParameter.Type, "Cues"),
            new QLabNetworkParameterAssignment(1, QLabEosParameter.SpecifyUser, "No"),
            new QLabNetworkParameterAssignment(2, QLabEosParameter.Command, "Run cue in specific list"),
            new QLabNetworkParameterAssignment(3, QLabEosParameter.List, "7"),
            new QLabNetworkParameterAssignment(4, QLabEosParameter.Cue, "12.5")
        ], parameters);
    }

    [Fact]
    public void User_shifts_following_parameter_indices()
    {
        var parameters = QLabEosNetworkCommand.RunCueInSpecificList("7", "12.5", " 3 ").BuildParameters();
        Assert.Equal(QLabEosParameter.User, parameters[2].Parameter);
        Assert.Equal("3", parameters[2].Value);
        Assert.Equal(5, parameters[^1].Index);
    }

    [Fact]
    public void RunCue_omits_list_parameter()
    {
        var parameters = QLabEosNetworkCommand.RunCue("1").BuildParameters();
        Assert.DoesNotContain(parameters, item => item.Parameter == QLabEosParameter.List);
        Assert.Equal(QLabEosCommand.RunCue, QLabEosNetworkCommand.RunCue("1").Command);
    }

    [Fact]
    public void Custom_type_and_command_are_extensible()
    {
        var command = new QLabEosNetworkCommand(new QLabEosTargetType("Custom"), false, null,
            new QLabEosCommand("Do thing"), null, null);
        Assert.Equal(["Custom", "No", "Do thing"], command.BuildParameters().Select(x => x.Value));
        Assert.Equal("Custom", command.Type.ToString());
        Assert.Equal("Do thing", command.Command.ToString());
    }

    [Fact]
    public void Defines_known_target_type_values()
    {
        Assert.Equal([
            "Cues", "Submasters", "Channels", "Groups", "Macros", "Presets", "Effects", "Snapshots",
            "Intensity Palettes", "Focus Palettes", "Color Palettes", "Beam Palettes"
        ], new[]
        {
            QLabEosTargetType.Cues, QLabEosTargetType.Submasters, QLabEosTargetType.Channels,
            QLabEosTargetType.Groups, QLabEosTargetType.Macros, QLabEosTargetType.Presets,
            QLabEosTargetType.Effects, QLabEosTargetType.Snapshots, QLabEosTargetType.IntensityPalettes,
            QLabEosTargetType.FocusPalettes, QLabEosTargetType.ColorPalettes, QLabEosTargetType.BeamPalettes
        }.Select(x => x.Value));
        Assert.Equal(6, Enum.GetValues<QLabEosParameter>().Length);
    }

    [Fact]
    public void Rejects_inconsistent_or_missing_required_values()
    {
        Assert.Throws<ArgumentException>(() => QLabEosNetworkCommand.RunCue(" "));
        Assert.Throws<ArgumentException>(() => QLabEosNetworkCommand.RunCueInSpecificList(" ", "1"));
        Assert.Throws<ArgumentException>(() => QLabEosNetworkCommand.RunCueInSpecificList("1", " "));
        Assert.Throws<ArgumentException>(() =>
            new QLabEosNetworkCommand(new QLabEosTargetType(" "), false, null, QLabEosCommand.RunCue, null, "1")
                .BuildParameters());
        Assert.Throws<ArgumentException>(() =>
            new QLabEosNetworkCommand(QLabEosTargetType.Cues, false, null, new QLabEosCommand(" "), null, "1")
                .BuildParameters());
        Assert.Throws<InvalidOperationException>(() =>
            new QLabEosNetworkCommand(QLabEosTargetType.Cues, true, null, QLabEosCommand.RunCue, null, "1")
                .BuildParameters());
        Assert.Throws<InvalidOperationException>(() =>
            new QLabEosNetworkCommand(QLabEosTargetType.Cues, false, "1", QLabEosCommand.RunCue, null, "1")
                .BuildParameters());
    }
}