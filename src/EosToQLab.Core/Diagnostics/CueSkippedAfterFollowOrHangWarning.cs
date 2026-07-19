namespace EosToQLab.Core.Diagnostics;

public sealed record CueSkippedAfterFollowOrHangWarning(string CueNumber) : EosWarning
{
    public override string Code => "QLAB_CUE_SKIPPED_AFTER_FOLLOW_OR_HANG";

    public override string Message =>
        $"EOS cue {CueNumber} was skipped because the previous cue in the same list has a follow or hang flag.";
}