namespace EosToQLab.Core.Diagnostics;

public sealed record Esf3dNoCueSequenceWarning : EosWarning
{
    public override string Code => "EOS_ESF3D_NO_CUE_SEQUENCE";
    public override string Message => "No plausible monotonic cue sequence was found in showdat.dat.";
}
