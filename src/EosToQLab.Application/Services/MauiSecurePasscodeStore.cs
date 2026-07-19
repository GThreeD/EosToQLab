namespace EosToQLab.Application.Services;

public sealed class MauiSecurePasscodeStore : IQLabPasscodeStore
{
    private const string KeyPrefix = "qlab.osc.passcode.";

    public async Task<string?> GetAsync(string workspaceId)
    {
        var key = BuildKey(workspaceId);
        try
        {
            return await SecureStorage.Default.GetAsync(key);
        }
        catch
        {
            // Secure storage can become unreadable after keychain or signing changes.
            // Remove only this workspace value so the user can enter it again.
            SecureStorage.Default.Remove(key);
            return null;
        }
    }

    public async Task SetAsync(string workspaceId, string? passcode)
    {
        var key = BuildKey(workspaceId);
        if (string.IsNullOrWhiteSpace(passcode))
        {
            SecureStorage.Default.Remove(key);
            return;
        }

        await SecureStorage.Default.SetAsync(key, passcode);
    }

    private static string BuildKey(string workspaceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);
        return KeyPrefix + workspaceId.Trim().ToUpperInvariant();
    }
}