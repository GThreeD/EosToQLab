namespace EosToQLab.Core.Diagnostics;

public sealed record CsvCuePartsMergedWarning(int PartCount) : EosWarning
{
    public override string Code => "EOS_CSV_CUE_PARTS_MERGED";
    public override string Message => $"{PartCount} cue part row(s) were merged into their parent cues.";
}
