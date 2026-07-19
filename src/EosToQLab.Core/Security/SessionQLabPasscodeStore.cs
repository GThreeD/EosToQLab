using System.Collections.Concurrent;

namespace EosToQLab.Core.Security;

public sealed class SessionQLabPasscodeStore(TimeProvider timeProvider) : IQLabPasscodeStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, CacheEntry> _entries =
        new(StringComparer.OrdinalIgnoreCase);

    public SessionQLabPasscodeStore() : this(TimeProvider.System)
    {
    }

    public Task<string?> GetAsync(string workspaceId)
    {
        var key = NormalizeWorkspaceId(workspaceId);
        if (!_entries.TryGetValue(key, out var entry)) return Task.FromResult<string?>(null);

        if (entry.ExpiresAtUtc <= timeProvider.GetUtcNow())
        {
            _entries.TryRemove(key, out _);
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(entry.Passcode);
    }

    public Task SetAsync(string workspaceId, string? passcode)
    {
        var key = NormalizeWorkspaceId(workspaceId);
        if (string.IsNullOrWhiteSpace(passcode))
        {
            _entries.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        _entries[key] = new CacheEntry(
            passcode,
            timeProvider.GetUtcNow().Add(Lifetime));

        return Task.CompletedTask;
    }

    private static string NormalizeWorkspaceId(string workspaceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);
        return workspaceId.Trim().ToUpperInvariant();
    }

    private sealed record CacheEntry(string Passcode, DateTimeOffset ExpiresAtUtc);
}