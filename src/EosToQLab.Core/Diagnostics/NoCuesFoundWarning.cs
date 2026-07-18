namespace EosToQLab.Core.Diagnostics;

public sealed record NoCuesFoundWarning : EosWarning
{
    public override string Code => "EOS_NO_CUES_FOUND";
    public override string Message => "No EOS cue records were found in the selected source.";
}
