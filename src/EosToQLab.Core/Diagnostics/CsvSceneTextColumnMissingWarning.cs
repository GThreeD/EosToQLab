namespace EosToQLab.Core.Diagnostics;

public sealed record CsvSceneTextColumnMissingWarning : EosWarning
{
    public override string Code => "EOS_CSV_SCENE_TEXT_COLUMN_MISSING";
    public override string Message => "The SCENE_TEXT column is missing. Scene memo cues cannot be created.";
}
