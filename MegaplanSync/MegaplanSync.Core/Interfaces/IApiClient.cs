namespace MegaplanSync.Core.Interfaces;

public interface IApiClient
{
    Task<string> GetEntitiesJsonAsync(string entityName, string jsonPayload = null);
    Task<string> GetEntityJsonAsync(string beforeIdUrlPart, long id, string afterIdUrlpart = null, string jsonPayload = null);
    Task<string> PostEntityJsonAsync(string entityName, long id, string jsonPayload);
}
