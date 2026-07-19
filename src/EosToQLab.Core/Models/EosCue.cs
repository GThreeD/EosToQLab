namespace EosToQLab.Core.Models;

public sealed record EosCue
{
    public required int SourceOrder { get; init; }
    public required int ListNumber { get; init; }
    public required string CueNumber { get; init; }
    public string? TargetDcid { get; init; }
    public string? Label { get; init; }
    public string? Follow { get; init; }
    public string? CueNotes { get; init; }
    public string? SceneText { get; init; }
    public EosSourceKind SourceKind { get; init; }

    public IReadOnlyDictionary<string, string?> AdditionalValues { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    public string DisplayCueNumber => ListNumber == 1 ? CueNumber : $"{ListNumber}/{CueNumber}";

    public bool HasFollowOrHang
    {
        get
        {
            var value = Follow?.Trim();
            return !string.IsNullOrEmpty(value)
                   && (value.StartsWith('F') || value.StartsWith('f')
                                             || value.StartsWith('H') || value.StartsWith('h'));
        }
    }
}