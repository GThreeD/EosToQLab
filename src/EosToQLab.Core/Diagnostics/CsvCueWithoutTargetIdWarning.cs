namespace EosToQLab.Core.Diagnostics;

public sealed record CsvCueWithoutTargetIdWarning(int RowNumber) : EosWarning
{
    public override string Code => "EOS_CSV_CUE_WITHOUT_TARGET_ID";
    public override string Message => $"The cue in CSV row {RowNumber} was ignored because TARGET_ID is empty.";
}