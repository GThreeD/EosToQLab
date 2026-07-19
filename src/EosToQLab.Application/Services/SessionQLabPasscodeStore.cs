using System.Collections.Concurrent;

namespace EosToQLab.Application.Services;

public sealed class SessionQLabPasscodeStore : IQLabPasscodeStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, CacheEntry> _entries =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<string?> GetAsync(string workspaceId)
    {
        var key = NormalizeWorkspaceId(workspaceId);
        if (!_entries.TryGetValue(key, out var entry))
        {
            return Task.FromResult<string?>(null);
        }

        if (entry.ExpiresAtUtc <= DateTimeOffset.UtcNow)
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
            DateTimeOffset.UtcNow.Add(Lifetime));

        return Task.CompletedTask;
    }

    private static string NormalizeWorkspaceId(string workspaceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);
        return workspaceId.Trim().ToUpperInvariant();
    }

    private sealed record CacheEntry(string Passcode, DateTimeOffset ExpiresAtUtc);
}
