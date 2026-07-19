namespace EosToQLab.Core.Diagnostics;

public sealed record CueImportedDisarmedAfterFollowOrHangWarning(string CueNumber) : EosWarning
{
    public override string Code => "EOS_FOLLOWED_CUE_DISARMED";

    public override string Message =>
        $"EOS cue {CueNumber} is triggered by a preceding follow/hang cue and was imported disarmed.";
}