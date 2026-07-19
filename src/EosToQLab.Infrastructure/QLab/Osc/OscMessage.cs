namespace EosToQLab.Infrastructure.QLab.Osc;

internal sealed record OscMessage(string Address, IReadOnlyList<object?> Arguments)
{
    public OscMessage(string address, params object?[] arguments) : this(address, (IReadOnlyList<object?>)arguments)
    {
    }
}