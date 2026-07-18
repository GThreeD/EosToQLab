namespace EosToQLab.Core.Diagnostics;

public sealed record CsvEndMarkerMissingWarning : EosWarning
{
    public override string Code => "EOS_CSV_END_MARKER_MISSING";
    public override string Message => "The END_TARGETS marker is missing. The importer read records until the end of the file.";
}
