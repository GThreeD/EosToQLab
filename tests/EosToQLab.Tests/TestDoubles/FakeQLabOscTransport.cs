namespace EosToQLab.Tests.TestDoubles;

internal sealed class FakeQLabOscTransport : IQLabOscTransport
{
    private readonly Queue<Func<OscMessage, QLabOscReply>> _replies = new();
    public List<OscMessage> Messages { get; } = [];
    public int ConnectCount { get; private set; }
    public int DisposeCount { get; private set; }
    public Exception? ConnectException { get; set; }
    public Exception? SendException { get; set; }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConnectCount++;
        if (ConnectException is not null) throw ConnectException;
        return Task.CompletedTask;
    }

    public Task<QLabOscReply> SendAsync(OscMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Messages.Add(message);
        if (SendException is not null) throw SendException;
        if (_replies.Count == 0) throw new InvalidOperationException("No fake QLab reply is queued.");
        return Task.FromResult(_replies.Dequeue()(message));
    }

    public ValueTask DisposeAsync()
    {
        DisposeCount++;
        return ValueTask.CompletedTask;
    }

    public void EnqueueReply(string address, string json)
    {
        _replies.Enqueue(_ => QLabOscReply.Parse(new OscMessage(address, json)));
    }

    public void EnqueueReply(Func<OscMessage, QLabOscReply> factory)
    {
        _replies.Enqueue(factory);
    }
}

internal sealed class FakeQLabOscTransportFactory(Func<IQLabOscTransport> create) : IQLabOscTransportFactory
{
    public int CreateCount { get; private set; }

    public IQLabOscTransport Create()
    {
        CreateCount++;
        return create();
    }
}