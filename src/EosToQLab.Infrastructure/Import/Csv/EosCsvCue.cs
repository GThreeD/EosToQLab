namespace EosToQLab.Infrastructure.Import.Csv;

public sealed class EosCsvCue
{
    [EosCsvColumn("TARGET_TYPE")] public string? TargetType { get; init; }

    [EosCsvColumn("TARGET_TYPE_AS_TEXT", Required = true)]
    public string TargetTypeAsText { get; init; } = string.Empty;

    [EosCsvColumn("TARGET_LIST_NUMBER", Required = true)]
    public int TargetListNumber { get; init; } = 1;

    [EosCsvColumn("TARGET_ID", Required = true)]
    public string TargetId { get; init; } = string.Empty;

    [EosCsvColumn("TARGET_DCID")] public string? TargetDcid { get; init; }

    [EosCsvColumn("PART_NUMBER")] public string? PartNumber { get; init; }

    [EosCsvColumn("LABEL")] public string? Label { get; init; }

    [EosCsvColumn("TIME_DATA")] public string? TimeData { get; init; }

    [EosCsvColumn("UP_DELAY")] public string? UpDelay { get; init; }

    [EosCsvColumn("DOWN_TIME")] public string? DownTime { get; init; }

    [EosCsvColumn("DOWN_DELAY")] public string? DownDelay { get; init; }

    [EosCsvColumn("FOCUS_TIME")] public string? FocusTime { get; init; }

    [EosCsvColumn("FOCUS_DELAY")] public string? FocusDelay { get; init; }

    [EosCsvColumn("COLOR_TIME")] public string? ColorTime { get; init; }

    [EosCsvColumn("COLOR_DELAY")] public string? ColorDelay { get; init; }

    [EosCsvColumn("BEAM_TIME")] public string? BeamTime { get; init; }

    [EosCsvColumn("BEAM_DELAY")] public string? BeamDelay { get; init; }

    [EosCsvColumn("DURATION")] public string? Duration { get; init; }

    [EosCsvColumn("ALERT_TIME")] public string? AlertTime { get; init; }

    [EosCsvColumn("MARK")] public string? Mark { get; init; }

    [EosCsvColumn("BLOCK")] public string? Block { get; init; }

    [EosCsvColumn("ASSERT")] public string? Assert { get; init; }

    [EosCsvColumn("ALL_FADE")] public string? AllFade { get; init; }

    [EosCsvColumn("PREHEAT")] public string? Preheat { get; init; }

    [EosCsvColumn("FOLLOW")] public string? Follow { get; init; }

    [EosCsvColumn("LINK")] public string? Link { get; init; }

    [EosCsvColumn("LOOP")] public string? Loop { get; init; }

    [EosCsvColumn("CURVE")] public string? Curve { get; init; }

    [EosCsvColumn("RATE")] public string? Rate { get; init; }

    [EosCsvColumn("EXTERNAL_LINKS")] public string? ExternalLinks { get; init; }

    [EosCsvColumn("EFFECTS")] public string? Effects { get; init; }

    [EosCsvColumn("MODE")] public string? Mode { get; init; }

    [EosCsvColumn("CUE_NOTES")] public string? CueNotes { get; init; }

    [EosCsvColumn("SCENE_TEXT")] public string? SceneText { get; init; }

    [EosCsvColumn("SCENE_END")] public string? SceneEnd { get; init; }

    [EosCsvColumn("WIDTH")] public string? Width { get; init; }

    [EosCsvColumn("HEIGHT")] public string? Height { get; init; }

    public bool IsPart => !string.IsNullOrWhiteSpace(PartNumber);
}