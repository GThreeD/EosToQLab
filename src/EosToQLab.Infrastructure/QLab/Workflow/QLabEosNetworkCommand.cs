namespace EosToQLab.Infrastructure.QLab.Workflow;

/// <summary>
///     String-backed EOS target type used by QLab's EOS network device description.
///     The type is intentionally extensible because QLab/ETC can add menu values without
///     requiring a breaking enum change in the importer.
/// </summary>
public readonly record struct QLabEosTargetType(string Value)
{
    public static QLabEosTargetType Cues { get; } = new("Cues");
    public static QLabEosTargetType Submasters { get; } = new("Submasters");
    public static QLabEosTargetType Channels { get; } = new("Channels");
    public static QLabEosTargetType Groups { get; } = new("Groups");
    public static QLabEosTargetType Macros { get; } = new("Macros");
    public static QLabEosTargetType Presets { get; } = new("Presets");
    public static QLabEosTargetType Effects { get; } = new("Effects");
    public static QLabEosTargetType Snapshots { get; } = new("Snapshots");
    public static QLabEosTargetType IntensityPalettes { get; } = new("Intensity Palettes");
    public static QLabEosTargetType FocusPalettes { get; } = new("Focus Palettes");
    public static QLabEosTargetType ColorPalettes { get; } = new("Color Palettes");
    public static QLabEosTargetType BeamPalettes { get; } = new("Beam Palettes");

    public override string ToString()
    {
        return Value;
    }
}

/// <summary>
///     String-backed command because the available command menu depends on <see cref="QLabEosTargetType" />.
/// </summary>
public readonly record struct QLabEosCommand(string Value)
{
    public static QLabEosCommand RunCue { get; } = new("Run cue");
    public static QLabEosCommand RunCueInSpecificList { get; } = new("Run cue in specific list");

    public override string ToString()
    {
        return Value;
    }
}

public enum QLabEosParameter
{
    Type,
    SpecifyUser,
    User,
    Command,
    List,
    Cue
}

/// <summary>
///     Declarative representation of the visible parameter stack in QLab's EOS network patch.
///     Parameter indices are generated in visible order, so enabling "Specify user" correctly
///     inserts the User field and shifts all following fields by one.
/// </summary>
public sealed record QLabEosNetworkCommand(
    QLabEosTargetType Type,
    bool SpecifyUser,
    string? User,
    QLabEosCommand Command,
    string? ListValue,
    string? CueValue)
{
    public static QLabEosNetworkCommand RunCue(
        string cue,
        string? user = null)
    {
        return new QLabEosNetworkCommand(
            QLabEosTargetType.Cues,
            !string.IsNullOrWhiteSpace(user),
            NullIfWhiteSpace(user),
            QLabEosCommand.RunCue,
            null,
            Required(cue, nameof(cue)));
    }

    public static QLabEosNetworkCommand RunCueInSpecificList(
        string list,
        string cue,
        string? user = null)
    {
        return new QLabEosNetworkCommand(
            QLabEosTargetType.Cues,
            !string.IsNullOrWhiteSpace(user),
            NullIfWhiteSpace(user),
            QLabEosCommand.RunCueInSpecificList,
            Required(list, nameof(list)),
            Required(cue, nameof(cue)));
    }

    public IReadOnlyList<QLabNetworkParameterAssignment> BuildParameters()
    {
        var type = Required(Type.Value, nameof(Type));
        var command = Required(Command.Value, nameof(Command));
        var user = NullIfWhiteSpace(User);
        if (SpecifyUser && user is null)
            throw new InvalidOperationException("An EOS user must be supplied when Specify user is Yes.");
        if (!SpecifyUser && user is not null)
            throw new InvalidOperationException("Specify user must be Yes when an EOS user is supplied.");

        var result = new List<QLabNetworkParameterAssignment>();
        Add(result, QLabEosParameter.Type, type);
        Add(result, QLabEosParameter.SpecifyUser, SpecifyUser ? "Yes" : "No");

        if (user is not null) Add(result, QLabEosParameter.User, user);

        Add(result, QLabEosParameter.Command, command);
        var list = NullIfWhiteSpace(ListValue);
        if (list is not null) Add(result, QLabEosParameter.List, list);

        var cue = NullIfWhiteSpace(CueValue);
        if (cue is not null) Add(result, QLabEosParameter.Cue, cue);

        return result;
    }

    private static void Add(
        List<QLabNetworkParameterAssignment> target,
        QLabEosParameter parameter,
        string value)
    {
        target.Add(new QLabNetworkParameterAssignment(target.Count, parameter, value));
    }

    private static string Required(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be empty.", parameterName)
            : value.Trim();
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}