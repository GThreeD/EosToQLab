namespace EosToQLab.Core.Diagnostics;

public sealed record EosShowArchiveLossTolerantParsingWarning : EosWarning
{
    public override string Code => "EOS_SHOW_ARCHIVE_LOSS_TOLERANT_PARSE";

    public override string Message =>
        "EOS show archive parsing is loss-tolerant because the proprietary showdat.dat object schema is not fully documented.";
}