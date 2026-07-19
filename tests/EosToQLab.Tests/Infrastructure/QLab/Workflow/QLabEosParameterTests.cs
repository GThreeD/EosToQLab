namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabEosParameterTests
{
    [Fact]
    public void Defines_visible_parameter_kinds()
    {
        Assert.Equal(
            [
                QLabEosParameter.Type,
                QLabEosParameter.SpecifyUser,
                QLabEosParameter.User,
                QLabEosParameter.Command,
                QLabEosParameter.List,
                QLabEosParameter.Cue
            ],
            Enum.GetValues<QLabEosParameter>());
    }
}