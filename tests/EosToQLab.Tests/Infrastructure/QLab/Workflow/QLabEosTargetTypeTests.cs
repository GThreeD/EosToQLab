namespace EosToQLab.Tests.Infrastructure.QLab.Workflow;

public sealed class QLabEosTargetTypeTests
{
    [Fact]
    public void Exposes_known_values_and_supports_future_custom_values()
    {
        var expected = new[]
        {
            "Cues", "Submasters", "Channels", "Groups", "Macros", "Presets", "Effects", "Snapshots",
            "Intensity Palettes", "Focus Palettes", "Color Palettes", "Beam Palettes"
        };
        var actual = new[]
        {
            QLabEosTargetType.Cues, QLabEosTargetType.Submasters, QLabEosTargetType.Channels,
            QLabEosTargetType.Groups, QLabEosTargetType.Macros, QLabEosTargetType.Presets,
            QLabEosTargetType.Effects, QLabEosTargetType.Snapshots, QLabEosTargetType.IntensityPalettes,
            QLabEosTargetType.FocusPalettes, QLabEosTargetType.ColorPalettes, QLabEosTargetType.BeamPalettes
        };

        Assert.Equal(expected, actual.Select(value => value.Value));
        Assert.Equal("Future type", new QLabEosTargetType("Future type").ToString());
    }
}