using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models;
using System.Net.Http.Headers;
using System.Text;

namespace MegaplanSync.ApiClient;

/// <summary>
/// HTTP-клиент API Мегаплана. MPIC использует только чтение сущностей (GET).
/// </summary>
public class MegaApiClient(ILogger logger, string tokenFile,
    string tokenExpAtFile, string baseApiUrl, string username, string password) : IApiClient
{
    public async Task<string?> GetEntitiesJsonAsync(string entityName, string? jsonPayload = null)
    {
        var token = new Token(logger, tokenFile, tokenExpAtFile);
        var accessToken = await token.GetAccessTokenAsync(baseApiUrl, username, password);
        var sb = new StringBuilder();
        sb.Append($"{baseApiUrl}/{entityName}");
        if (jsonPayload != null)
        {
            sb.Append($"?");
            string encodedParams = Uri.EscapeDataString(jsonPayload);
            sb.Append(encodedParams);
        }
        var url = sb.ToString();

        using HttpClient client = new();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            logger.LogDebug($"Ответ от api получен");
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            string errorMessage = await response.Content.ReadAsStringAsync();
            logger.LogDebug($"Ошибка: {response.StatusCode}, {errorMessage}");
            return null;
        }
    }
}
