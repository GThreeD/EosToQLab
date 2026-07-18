namespace EosToQLab.Core.Diagnostics;

public sealed record Esf3dFollowNotDecodedWarning : EosWarning
{
    public override string Code => "EOS_ESF3D_FOLLOW_NOT_DECODED";
    public override string Message => "Follow and hang fields are not decoded from showdat.dat and therefore remain empty for ESF3D imports.";
}
