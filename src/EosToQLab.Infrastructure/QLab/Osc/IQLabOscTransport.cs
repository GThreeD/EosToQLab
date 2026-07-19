namespace EosToQLab.Infrastructure.QLab.Osc;

internal interface IQLabOscTransport : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task<QLabOscReply> SendAsync(OscMessage message, CancellationToken cancellationToken);
}