namespace EosToQLab.Tests.TestDoubles;

internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; private set; } = utcNow;

    public override DateTimeOffset GetUtcNow()
    {
        return UtcNow;
    }

    public void Advance(TimeSpan value)
    {
        UtcNow = UtcNow.Add(value);
    }
}