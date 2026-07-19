using System.Text.Json;

namespace EosToQLab.Tests.Infrastructure.QLab.Osc;

public sealed class QLabOscReplyTests
{
    [Fact]
    public void Parses_ok_reply_and_exposes_data()
    {
        using var reply = QLabOscReply.Parse(new OscMessage("/reply/test", "{\"status\":\"ok\",\"data\":\"value\"}"));
        Assert.Equal("/reply/test", reply.Address);
        Assert.Equal("ok", reply.Status);
        Assert.Equal("value", reply.Data.GetString());
        Assert.Equal(reply.Document.RootElement.GetRawText(), reply.Root.GetRawText());
        reply.EnsureOk("Workspace");
    }

    [Theory]
    [InlineData("{\"status\":\"denied\"}")]
    [InlineData("{\"status\":\"badpass\"}")]
    [InlineData("{\"status\":\"error\",\"data\":\"badpass\"}")]
    public void Converts_access_denied_shapes(string json)
    {
        using var reply = QLabOscReply.Parse(new OscMessage("/reply/test", json));
        Assert.Throws<QLabAccessDeniedException>(() => reply.EnsureOk("Workspace"));
    }

    [Fact]
    public void Rejects_non_ok_and_malformed_replies()
    {
        using var error = QLabOscReply.Parse(new OscMessage("/reply/test", "{\"status\":\"error\"}"));
        Assert.Throws<QLabUnexpectedReplyException>(() => error.EnsureOk("Workspace"));
        Assert.Throws<QLabUnexpectedReplyException>(() => QLabOscReply.Parse(new OscMessage("/not-reply", "{}")));
        Assert.Throws<QLabUnexpectedReplyException>(() => QLabOscReply.Parse(new OscMessage("/reply/test")));
        Assert.Throws<QLabUnexpectedReplyException>(() => QLabOscReply.Parse(new OscMessage("/reply/test", 1)));
        Assert.Throws<QLabOscPacketException>(() => QLabOscReply.Parse(new OscMessage("/reply/test", "{")));
    }

    [Fact]
    public void Dispose_releases_the_json_document()
    {
        var reply = QLabOscReply.Parse(new OscMessage("/reply/test", "{\"status\":\"ok\"}"));

        reply.Dispose();

        Assert.Throws<ObjectDisposedException>(() => reply.Root.GetRawText());
    }

    [Fact]
    public void Missing_fields_return_empty_or_undefined()
    {
        using var reply = QLabOscReply.Parse(new OscMessage("/reply/test", "{}"));
        Assert.Equal(string.Empty, reply.Status);
        Assert.Equal(JsonValueKind.Undefined, reply.Data.ValueKind);
    }
}