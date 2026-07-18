namespace EosToQLab.Core.Diagnostics;

public sealed record Esf3dLossTolerantParsingWarning : EosWarning
{
    public override string Code => "EOS_ESF3D_LOSS_TOLERANT_PARSE";
    public override string Message => "ESF3D parsing is loss-tolerant because the proprietary showdat.dat object schema is not fully documented.";
}
