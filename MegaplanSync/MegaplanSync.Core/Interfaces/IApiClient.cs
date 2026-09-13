namespace MegaplanSync.Core.Interfaces;

public interface IApiClient
{
    Task<string?> GetEntitiesJsonAsync(string entityName, string? jsonPayload = null);
}
