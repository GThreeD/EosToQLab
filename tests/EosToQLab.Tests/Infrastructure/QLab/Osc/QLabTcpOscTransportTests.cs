using System.Net;
using System.Net.Sockets;

namespace EosToQLab.Tests.Infrastructure.QLab.Osc;

public sealed class QLabTcpOscTransportTests
{
    [Fact]
    public async Task Connects_sends_request_ignores_non_reply_and_returns_reply()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = RunReplyServerAsync(listener, cancellationToken);

        await using (var transport = new QLabTcpOscTransport(
                         "127.0.0.1",
                         port,
                         TimeSpan.FromSeconds(2)))
        {
            await transport.ConnectAsync(cancellationToken);

            using var reply = await transport.SendAsync(
                new OscMessage("/request"),
                cancellationToken);

            Assert.Equal("done", reply.Data.GetString());
        }

        await server;
    }

    private static async Task RunReplyServerAsync(
        TcpListener listener,
        CancellationToken cancellationToken)
    {
        using var client =
            await listener.AcceptTcpClientAsync(cancellationToken);

        await using var stream = client.GetStream();

        var reader = new SlipStreamReader(stream);

        var requestFrame =
            await reader.ReadFrameAsync(cancellationToken);

        var request = OscCodec.Decode(requestFrame);

        Assert.Equal("/request", request.Address);

        await stream.WriteAsync(
            SlipCodec.Frame(
                OscCodec.Encode(
                    new OscMessage("/other", 1))),
            cancellationToken);

        await stream.WriteAsync(
            SlipCodec.Frame(
                OscCodec.Encode(
                    new OscMessage(
                        "/reply/request",
                        "{\"status\":\"ok\",\"data\":\"done\"}"))),
            cancellationToken);

        await stream.FlushAsync(cancellationToken);

        var disconnectFrame =
            await reader.ReadFrameAsync(cancellationToken);

        var disconnect = OscCodec.Decode(disconnectFrame);

        Assert.Equal("/disconnect", disconnect.Address);
    }

    [Fact]
    public async Task Send_before_connect_is_rejected()
    {
        await using var transport = new QLabTcpOscTransport(timeout: TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAsync<QLabTransportNotConnectedException>(() =>
            transport.SendAsync(new OscMessage("/request"), CancellationToken.None));
    }

    [Fact]
    public async Task Connection_failure_is_wrapped()
    {
        var port = ReserveAndReleasePort();
        await using var transport = new QLabTcpOscTransport("127.0.0.1", port, TimeSpan.FromMilliseconds(200));
        await Assert.ThrowsAsync<QLabConnectionException>(() => transport.ConnectAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Missing_reply_times_out()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = RunServerAsync(listener, cancellationToken);

        await using var transport = new QLabTcpOscTransport(
            "127.0.0.1",
            port,
            TimeSpan.FromMilliseconds(50));

        await transport.ConnectAsync(cancellationToken);

        await Assert.ThrowsAsync<QLabRequestTimeoutException>(() =>
            transport.SendAsync(
                new OscMessage("/request"),
                cancellationToken));

        await server;
    }

    private static async Task RunServerAsync(
        TcpListener listener,
        CancellationToken cancellationToken)
    {
        using var client = await listener.AcceptTcpClientAsync(cancellationToken);
        await Task.Delay(500, cancellationToken);
    }

    [Fact]
    public async Task Dispose_without_connection_is_safe()
    {
        var transport = new QLabTcpOscTransport();
        await transport.DisposeAsync();
    }

    private static int ReserveAndReleasePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}