namespace EosToQLab.Core.Diagnostics;

public sealed record CsvFollowColumnMissingWarning : EosWarning
{
    public override string Code => "EOS_CSV_FOLLOW_COLUMN_MISSING";
    public override string Message => "The FOLLOW column is missing. Follow and hang relationships cannot be evaluated.";
}
