using EosToQLab.Core.Models;

namespace EosToQLab.Application.Models;

public sealed class CuePreviewRow
{
    private readonly EosCue _source;

    private CuePreviewRow(EosCue source)
    {
        _source = source;
        IsSelected = source.ImportEnabled;
        Label = source.Label ?? string.Empty;
        Notes = source.CueNotes ?? string.Empty;
        Scene = source.SceneText ?? string.Empty;
    }

    public int SourceOrder => _source.SourceOrder;
    public string DisplayCueNumber => _source.DisplayCueNumber;
    public string? Follow => _source.Follow;
    public bool IsSelected { get; set; }
    public string Label { get; set; }
    public string Notes { get; set; }
    public string Scene { get; set; }

    public static CuePreviewRow FromCue(EosCue cue)
    {
        ArgumentNullException.ThrowIfNull(cue);
        return new CuePreviewRow(cue);
    }

    public EosCue ToCue()
    {
        return _source with
        {
            ImportEnabled = IsSelected,
            Label = NullIfWhiteSpace(Label),
            CueNotes = NullIfWhiteSpace(Notes),
            SceneText = NullIfWhiteSpace(Scene)
        };
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}