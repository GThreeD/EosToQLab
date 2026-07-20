namespace EosToQLab.Core.Diagnostics;

public sealed record EosShowArchiveNoCueSequenceWarning : EosWarning
{
    public override string Code => "EOS_SHOW_ARCHIVE_NO_CUE_SEQUENCE";
    public override string Message => "No plausible monotonic cue sequence was found in showdat.dat.";
}