namespace EosToQLab.Application.Services;

public interface IQLabPasscodeStore
{
    Task<string?> GetAsync(string workspaceId);
    Task SetAsync(string workspaceId, string? passcode);
}
