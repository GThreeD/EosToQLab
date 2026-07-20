namespace EosToQLab.Core.Diagnostics;

public sealed record EosShowArchiveVersionNotTestedWarning(string? Format, string? Version) : EosWarning
{
    public override string Code => "EOS_SHOW_ARCHIVE_VERSION_NOT_TESTED";

    public override string Message => string.IsNullOrWhiteSpace(Format) || string.IsNullOrWhiteSpace(Version)
        ? "The EOS show archive has no readable version.json. This archive version is not covered by the bundled compatibility fixtures and may be incompatible."
        : $"EOS show archive format '{Format}' version '{Version}' is not covered by the bundled compatibility fixtures and may be incompatible.";
}
