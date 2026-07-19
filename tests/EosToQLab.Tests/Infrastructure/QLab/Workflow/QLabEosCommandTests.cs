namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabEosCommandTests
{
    [Fact]
    public void Exposes_known_and_custom_commands()
    {
        Assert.Equal("Run cue", QLabEosCommand.RunCue.Value);
        Assert.Equal("Run cue in specific list", QLabEosCommand.RunCueInSpecificList.Value);
        Assert.Equal("Future command", new QLabEosCommand("Future command").ToString());
    }
}