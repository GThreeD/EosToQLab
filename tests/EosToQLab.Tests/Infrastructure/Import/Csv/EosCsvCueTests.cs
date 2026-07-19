namespace EosToQLab.Tests.Infrastructure.Import.Csv;

public sealed class EosCsvCueTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("1", true)]
    public void IsPart_depends_on_part_number(string? partNumber, bool expected)
    {
        Assert.Equal(expected, new EosCsvCue { PartNumber = partNumber }.IsPart);
    }

    [Fact]
    public void Defaults_are_valid()
    {
        var cue = new EosCsvCue();
        Assert.Equal(string.Empty, cue.TargetTypeAsText);
        Assert.Equal(1, cue.TargetListNumber);
        Assert.Equal(string.Empty, cue.TargetId);
    }

    [Fact]
    public void Stores_every_exported_EOS_column()
    {
        var cue = new EosCsvCue
        {
            TargetType = "target-type",
            TargetTypeAsText = "Cue",
            TargetListNumber = 2,
            TargetId = "83.1",
            TargetDcid = "dcid",
            PartNumber = "1",
            Label = "label",
            TimeData = "time",
            UpDelay = "up-delay",
            DownTime = "down-time",
            DownDelay = "down-delay",
            FocusTime = "focus-time",
            FocusDelay = "focus-delay",
            ColorTime = "color-time",
            ColorDelay = "color-delay",
            BeamTime = "beam-time",
            BeamDelay = "beam-delay",
            Duration = "duration",
            AlertTime = "alert-time",
            Mark = "mark",
            Block = "block",
            Assert = "assert",
            AllFade = "all-fade",
            Preheat = "preheat",
            Follow = "F1",
            Link = "link",
            Loop = "loop",
            Curve = "curve",
            Rate = "rate",
            ExternalLinks = "external-links",
            Effects = "effects",
            Mode = "mode",
            CueNotes = "notes",
            SceneText = "scene",
            SceneEnd = "scene-end",
            Width = "width",
            Height = "height"
        };

        var expected = new Dictionary<string, object?>
        {
            [nameof(EosCsvCue.TargetType)] = "target-type",
            [nameof(EosCsvCue.TargetTypeAsText)] = "Cue",
            [nameof(EosCsvCue.TargetListNumber)] = 2,
            [nameof(EosCsvCue.TargetId)] = "83.1",
            [nameof(EosCsvCue.TargetDcid)] = "dcid",
            [nameof(EosCsvCue.PartNumber)] = "1",
            [nameof(EosCsvCue.Label)] = "label",
            [nameof(EosCsvCue.TimeData)] = "time",
            [nameof(EosCsvCue.UpDelay)] = "up-delay",
            [nameof(EosCsvCue.DownTime)] = "down-time",
            [nameof(EosCsvCue.DownDelay)] = "down-delay",
            [nameof(EosCsvCue.FocusTime)] = "focus-time",
            [nameof(EosCsvCue.FocusDelay)] = "focus-delay",
            [nameof(EosCsvCue.ColorTime)] = "color-time",
            [nameof(EosCsvCue.ColorDelay)] = "color-delay",
            [nameof(EosCsvCue.BeamTime)] = "beam-time",
            [nameof(EosCsvCue.BeamDelay)] = "beam-delay",
            [nameof(EosCsvCue.Duration)] = "duration",
            [nameof(EosCsvCue.AlertTime)] = "alert-time",
            [nameof(EosCsvCue.Mark)] = "mark",
            [nameof(EosCsvCue.Block)] = "block",
            [nameof(EosCsvCue.Assert)] = "assert",
            [nameof(EosCsvCue.AllFade)] = "all-fade",
            [nameof(EosCsvCue.Preheat)] = "preheat",
            [nameof(EosCsvCue.Follow)] = "F1",
            [nameof(EosCsvCue.Link)] = "link",
            [nameof(EosCsvCue.Loop)] = "loop",
            [nameof(EosCsvCue.Curve)] = "curve",
            [nameof(EosCsvCue.Rate)] = "rate",
            [nameof(EosCsvCue.ExternalLinks)] = "external-links",
            [nameof(EosCsvCue.Effects)] = "effects",
            [nameof(EosCsvCue.Mode)] = "mode",
            [nameof(EosCsvCue.CueNotes)] = "notes",
            [nameof(EosCsvCue.SceneText)] = "scene",
            [nameof(EosCsvCue.SceneEnd)] = "scene-end",
            [nameof(EosCsvCue.Width)] = "width",
            [nameof(EosCsvCue.Height)] = "height"
        };

        foreach (var pair in expected)
        {
            var property = typeof(EosCsvCue).GetProperty(pair.Key)!;
            Assert.Equal(pair.Value, property.GetValue(cue));
        }
    }
}