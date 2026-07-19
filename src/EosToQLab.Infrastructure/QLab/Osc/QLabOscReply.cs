using System.Text.Json;
using EosToQLab.Core.Exceptions;

namespace EosToQLab.Infrastructure.QLab.Osc;

internal sealed class QLabOscReply : IDisposable
{
    private QLabOscReply(string address, JsonDocument document)
    {
        Address = address;
        Document = document;
    }

    public string Address { get; }
    public JsonDocument Document { get; }
    public JsonElement Root => Document.RootElement;

    public string Status => Root.TryGetProperty(QLabProtocol.Reply.StatusField, out var status)
        ? status.GetString() ?? string.Empty
        : string.Empty;

    public JsonElement Data => Root.TryGetProperty(QLabProtocol.Reply.DataField, out var data) ? data : default;

    public static QLabOscReply Parse(OscMessage message)
    {
        if (!message.Address.StartsWith(QLabProtocol.Addresses.ReplyPrefix, StringComparison.Ordinal))
            throw new QLabUnexpectedReplyException(message.Address, "The OSC message is not a QLab reply.");

        if (message.Arguments.Count == 0 || message.Arguments[0] is not string json)
            throw new QLabUnexpectedReplyException(message.Address,
                "The QLab reply does not contain a JSON string argument.");

        try
        {
            return new QLabOscReply(message.Address, JsonDocument.Parse(json));
        }
        catch (JsonException exception)
        {
            throw new QLabOscPacketException("The JSON payload in the QLab reply is invalid.", exception);
        }
    }

    public void EnsureOk(string workspaceName)
    {
        var dataText = Data.ValueKind == JsonValueKind.String ? Data.GetString() : null;
        if (string.Equals(Status, QLabProtocol.Reply.DeniedStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Status, QLabProtocol.Reply.BadPassStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(dataText, QLabProtocol.Reply.BadPassStatus, StringComparison.OrdinalIgnoreCase))
            throw new QLabAccessDeniedException(workspaceName);

        if (string.Equals(Status, QLabProtocol.Reply.OkStatus, StringComparison.OrdinalIgnoreCase)) return;

        throw new QLabUnexpectedReplyException(Address, Root.GetRawText());
    }

    public void Dispose()
    {
        Document.Dispose();
    }
}