namespace EosToQLab.Infrastructure.QLab;

public enum QLabCueType
{
    CueList,
    Memo,
    Network
}

public enum QLabCueProperty
{
    Name,
    Number,
    Notes,
    NetworkPatchId
}

public enum QLabWorkspaceProperty
{
    CurrentCueListId
}

public enum QLabNetworkParameter
{
    Category,
    Action,
    Description,
    CueListNumber,
    CueNumber
}

internal static class QLabProtocol
{
    internal static class Addresses
    {
        internal const string Workspaces = "/workspaces";
        internal const string AlwaysReply = "/alwaysReply";
        internal const string ReplyPrefix = "/reply";
        internal const string Disconnect = "/disconnect";

        internal static string Workspace(string workspaceId, string command) =>
            $"/workspace/{workspaceId}/{command}";

        internal static string Cue(string workspaceId, string cueId, string property) =>
            $"/workspace/{workspaceId}/cue_id/{cueId}/{property}";
    }

    internal static class Reply
    {
        internal const string StatusField = "status";
        internal const string DataField = "data";
        internal const string OkStatus = "ok";
        internal const string DeniedStatus = "denied";
        internal const string BadPassStatus = "badpass";
    }

    internal static class WorkspaceCommands
    {
        internal const string Connect = "connect";
        internal const string CueListsShallow = "cueLists/shallow";
        internal const string NewCue = "new";
        internal const string Save = "save";
        internal const string Undo = "undo";
        internal const string NetworkPatchList = "settings/network/patchList";

        internal static string DeleteById(string cueId) => $"delete_id/{cueId}";
    }

    internal static string CueTypeName(QLabCueType cueType) => cueType switch
    {
        QLabCueType.CueList => "cue list",
        QLabCueType.Memo => "memo",
        QLabCueType.Network => "network",
        _ => throw new ArgumentOutOfRangeException(nameof(cueType), cueType, null)
    };

    internal static string CuePropertyName(QLabCueProperty property) => property switch
    {
        QLabCueProperty.Name => "name",
        QLabCueProperty.Number => "number",
        QLabCueProperty.Notes => "notes",
        QLabCueProperty.NetworkPatchId => "networkPatchID",
        _ => throw new ArgumentOutOfRangeException(nameof(property), property, null)
    };

    internal static string WorkspacePropertyName(QLabWorkspaceProperty property) => property switch
    {
        QLabWorkspaceProperty.CurrentCueListId => "currentCueListID",
        _ => throw new ArgumentOutOfRangeException(nameof(property), property, null)
    };

    internal static int NetworkParameterIndex(QLabNetworkParameter parameter) => parameter switch
    {
        QLabNetworkParameter.Category => 0,
        QLabNetworkParameter.Action => 1,
        QLabNetworkParameter.Description => 2,
        QLabNetworkParameter.CueListNumber => 3,
        QLabNetworkParameter.CueNumber => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(parameter), parameter, null)
    };

    internal static string NetworkParameterProperty(QLabNetworkParameter parameter) =>
        $"parameterValue/{NetworkParameterIndex(parameter)}";

    internal static bool IsEosNetworkPatchType(string? patchType) =>
        string.IsNullOrWhiteSpace(patchType)
        || patchType.Contains("eos", StringComparison.OrdinalIgnoreCase);
}

internal static class QLabEosNetworkCommand
{
    internal const string Category = "Cues";
    internal const string Action = "No";
    internal const string Description = "Run cue in specific list";
}
