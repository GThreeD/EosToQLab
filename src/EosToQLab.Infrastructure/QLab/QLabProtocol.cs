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
    Armed,
    SkipIfDisarmed,
    NetworkPatchId
}

public enum QLabWorkspaceProperty
{
    CurrentCueListId
}

internal static class QLabProtocol
{
    internal static string CueTypeName(QLabCueType cueType)
    {
        return cueType switch
        {
            QLabCueType.CueList => "cue list",
            QLabCueType.Memo => "memo",
            QLabCueType.Network => "network",
            _ => throw new ArgumentOutOfRangeException(nameof(cueType), cueType, null)
        };
    }

    internal static string CuePropertyName(QLabCueProperty property)
    {
        return property switch
        {
            QLabCueProperty.Name => "name",
            QLabCueProperty.Number => "number",
            QLabCueProperty.Notes => "notes",
            QLabCueProperty.Armed => "armed",
            QLabCueProperty.SkipIfDisarmed => "skipIfDisarmed",
            QLabCueProperty.NetworkPatchId => "networkPatchID",
            _ => throw new ArgumentOutOfRangeException(nameof(property), property, null)
        };
    }

    internal static string WorkspacePropertyName(QLabWorkspaceProperty property)
    {
        return property switch
        {
            QLabWorkspaceProperty.CurrentCueListId => "currentCueListID",
            _ => throw new ArgumentOutOfRangeException(nameof(property), property, null)
        };
    }

    internal static string NetworkParameterProperty(int parameterIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(parameterIndex);
        return $"parameterValue/{parameterIndex}";
    }

    internal static bool IsEosNetworkPatchType(string? patchType)
    {
        return string.IsNullOrWhiteSpace(patchType)
               || patchType.Contains("eos", StringComparison.OrdinalIgnoreCase);
    }

    internal static class Addresses
    {
        internal const string Workspaces = "/workspaces";
        internal const string AlwaysReply = "/alwaysReply";
        internal const string ReplyPrefix = "/reply";
        internal const string Disconnect = "/disconnect";

        internal static string Workspace(string workspaceId, string command)
        {
            return $"/workspace/{workspaceId}/{command}";
        }

        internal static string Cue(string workspaceId, string cueId, string property)
        {
            return $"/workspace/{workspaceId}/cue_id/{cueId}/{property}";
        }
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

        internal static string DeleteById(string cueId)
        {
            return $"delete_id/{cueId}";
        }
    }
}