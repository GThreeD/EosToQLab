namespace EosToQLab.Core.Security;

public interface IQLabPasscodeStore
{
    Task<string?> GetAsync(string workspaceId);
    Task SetAsync(string workspaceId, string? passcode);
}