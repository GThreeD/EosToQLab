namespace EosToQLab.Core.Diagnostics;

public sealed record Esf3dFollowNotDecodedWarning(string CueNumber) : EosWarning
{
    public override string Code => "EOS_ESF3D_FOLLOW_NOT_DECODED";

    public override string Message =>
        $"The follow/hang value of EOS cue {CueNumber} uses an unsupported showdat.dat encoding and was left empty.";
}