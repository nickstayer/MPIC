using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models;
using System.Net.Http.Headers;
using System.Text;

namespace MegaplanSync.ApiClient;

public class MegaApiClient(ILogger logger, string tokenFile, 
    string tokenExpAtFile, string baseApiUrl, string username, string password) : IApiClient
{
    public async Task<string> GetEntitiesJsonAsync(string entityName, string jsonPayload = null)
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

    public async Task<string> GetEntityJsonAsync(string beforeIdUrlPart, long id, string afterIdUrlpart = null, string jsonPayload = null)
    {
        var token = new Token(logger, tokenFile, tokenExpAtFile);
        var accessToken = await token.GetAccessTokenAsync(baseApiUrl, username, password);
        var sb = new StringBuilder();
        sb.Append($"{baseApiUrl}/{beforeIdUrlPart}/{id}");
        if(afterIdUrlpart != null)
        {
            sb.Append($"/{afterIdUrlpart}");
        }
        
        if(jsonPayload != null)
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

    public async Task<string> PostEntityJsonAsync(string entityUrlPart, long id, string jsonPayload)
    {
        var token = new Token(logger, tokenFile, tokenExpAtFile);
        var accessToken = await token.GetAccessTokenAsync(baseApiUrl, username, password);

        var url = $"{baseApiUrl}/{entityUrlPart}/{id}";

        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        HttpResponseMessage response = await client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            logger.LogDebug($"Ответ от API получен. Сущность {entityUrlPart} {id} обновлена.");
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            string errorMessage = await response.Content.ReadAsStringAsync();
            logger.LogError($"Ошибка: {response.StatusCode} при обновлении {entityUrlPart}/{id}, Сообщение: {errorMessage}");
            return null;
        }
    }
}
