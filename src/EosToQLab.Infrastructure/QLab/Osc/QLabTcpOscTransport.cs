using System.Net.Sockets;
using EosToQLab.Core.Exceptions;

namespace EosToQLab.Infrastructure.QLab.Osc;

internal sealed class QLabTcpOscTransport(
    string host = "127.0.0.1",
    int port = 53000,
    TimeSpan? timeout = null) : IQLabOscTransport
{
    private readonly TcpClient _client = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly TimeSpan _timeout = timeout ?? TimeSpan.FromSeconds(10);
    private NetworkStream? _stream;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(_timeout);
            await _client.ConnectAsync(host, port, timeoutSource.Token);
            _stream = _client.GetStream();
        }
        catch (EosToQLabException)
        {
            throw;
        }
        catch (Exception exception) when (exception is SocketException or IOException or OperationCanceledException)
        {
            throw new QLabConnectionException(host, port, exception);
        }
    }

    public async Task<QLabOscReply> SendAsync(OscMessage message, CancellationToken cancellationToken)
    {
        var stream = _stream ?? throw new QLabTransportNotConnectedException();
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            var frame = SlipCodec.Frame(OscCodec.Encode(message));
            await stream.WriteAsync(frame, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(_timeout);
            while (true)
            {
                var replyFrame = await SlipCodec.ReadFrameAsync(stream, timeoutSource.Token);
                var decoded = OscCodec.Decode(replyFrame);
                if (!decoded.Address.StartsWith(QLabProtocol.Addresses.ReplyPrefix, StringComparison.Ordinal)) continue;
                return QLabOscReply.Parse(decoded);
            }
        }
        catch (EosToQLabException)
        {
            throw;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new QLabRequestTimeoutException(message.Address, _timeout, exception);
        }
        catch (Exception exception) when (exception is IOException or SocketException)
        {
            throw new QLabTransportException(message.Address, exception);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_stream is not null)
        {
            try
            {
                var frame = SlipCodec.Frame(OscCodec.Encode(new OscMessage(QLabProtocol.Addresses.Disconnect)));
                await _stream.WriteAsync(frame);
                await _stream.FlushAsync();
            }
            catch
            {
                // The connection is being disposed; disconnect failures are not actionable.
            }

            await _stream.DisposeAsync();
        }

        _client.Dispose();
        _sendLock.Dispose();
    }
}