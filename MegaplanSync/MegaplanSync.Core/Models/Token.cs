using System.Text.Json;
using System.Text.Json.Serialization;
using MegaplanSync.Core.Interfaces;

namespace MegaplanSync.Core.Models;

public class Token
{
    private readonly ILogger _logger;
    private readonly string _tokenFile;
    private readonly string _tokenExpAtFile;

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonIgnore]
    public DateTime ExpiresAt { get; private set; }

    private static readonly HttpClient _httpClient = new HttpClient();

    public Token() { }

    public Token(ILogger logger, string tokenFile, string tokenExpAtFile) : this()
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenFile = tokenFile ?? throw new ArgumentNullException(nameof(tokenFile));
        _tokenExpAtFile = tokenExpAtFile ?? throw new ArgumentNullException(nameof(tokenExpAtFile));
    }


    /// <summary>
    /// Получает новый токен аутентификации из API Мегаплана.
    /// </summary>
    /// <param name="apiUrl">URL API для получения токена.</param>
    /// <param name="username">Имя пользователя.</param>
    /// <param name="password">Пароль пользователя.</param>
    /// <returns>Новый объект Token или null в случае ошибки.</returns>
    private async Task<Token?> GetTokenFromApiAsync(string apiUrl, string username, string password)
    {
        _logger.LogDebug($"Получаю токен из API по адресу: {apiUrl}");

        try
        {
            var formData = new Dictionary<string, string>
            {
                { "username", username },
                { "password", password },
                { "grant_type", "password" }
            };

            using var content = new FormUrlEncodedContent(formData);
            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<Token>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (tokenResponse != null)
                {
                    tokenResponse.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                    _logger.LogInformation("Токен успешно получен из API.");
                    return tokenResponse;
                }
                else
                {
                    _logger.LogError("API вернуло некорректный или пустой ответ при запросе токена.");
                    return null;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Ошибка HTTP при получении токена: {response.StatusCode}. Ответ: {errorContent}");
                return null;
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError($"Ошибка запроса к API токена: {httpEx.Message}", httpEx);
            return null;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError($"Ошибка десериализации ответа токена: {jsonEx.Message}", jsonEx);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Неизвестная ошибка при получении токена из API: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Сохраняет текущий объект токена в файлы.
    /// </summary>
    private void CacheCurrentToken()
    {
        try
        {
            var tokenJson = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_tokenFile, tokenJson);
            File.WriteAllText(_tokenExpAtFile, ExpiresAt.ToString("o")); // "o" для ISO 8601
            _logger.LogInformation($"Токен успешно сохранен в '{_tokenFile}' и время истечения в '{_tokenExpAtFile}'.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Ошибка при кэшировании токена: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Читает кэшированный токен из файла.
    /// </summary>
    /// <returns>Объект Token из кэша или null, если не удалось прочитать.</returns>
    private Token? ReadCachedToken()
    {
        if (!File.Exists(_tokenFile))
        {
            return null;
        }

        try
        {
            var tokenCachedJson = File.ReadAllText(_tokenFile);
            var cachedToken = JsonSerializer.Deserialize<Token>(tokenCachedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (cachedToken != null)
            {
                if (File.Exists(_tokenExpAtFile))
                {
                    var expiresAtString = File.ReadAllText(_tokenExpAtFile);
                    if (DateTime.TryParse(expiresAtString, out DateTime expiresAt))
                    {
                        cachedToken.ExpiresAt = expiresAt;
                        return cachedToken;
                    }
                    else
                    {
                        _logger.LogError($"Не удалось распарсить время истечения кэшированного токена из '{_tokenExpAtFile}'. Кэш будет проигнорирован.");
                        return null;
                    }
                }
                else
                {
                    _logger.LogWarning($"Файл срока действия токена не найден: '{_tokenExpAtFile}'. Кэш будет проигнорирован.");
                    return null;
                }
            }
            else
            {
                _logger.LogError("Не удалось десериализовать кэшированный токен из файла.");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Ошибка при чтении кэшированного токена: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Проверяет, актуален ли кэшированный токен.
    /// </summary>
    /// <returns>True, если токен актуален, иначе False.</returns>
    private bool IsTokenActual()
    {
        // Здесь используем ExpiresAt из текущего экземпляра токена
        // Если вы хотите проверять только по файлу tokenExpAtFile без загрузки всего токена:
        if (!File.Exists(_tokenExpAtFile))
        {
            _logger.LogDebug("Файл срока действия токена не найден, токен неактуален.");
            return false;
        }
        try
        {
            var tokenExpDataStr = File.ReadAllText(_tokenExpAtFile);
            if (DateTime.TryParse(tokenExpDataStr, out DateTime tokenExpData))
            {
                // Используем DateTime.UtcNow для сравнения, так как ExpiresAt сохраняется в UTC
                bool isActual = DateTime.Now < tokenExpData.AddMinutes(-5); // 5 минут запас
                return isActual;
            }
            else
            {
                _logger.LogError($"Не удалось распарсить время истечения токена из '{_tokenExpAtFile}'. Токен неактуален.");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Ошибка при проверке актуальности токена: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Основной метод для получения актуального AccessToken.
    /// </summary>
    /// <param name="apiUrl">URL API для получения токена.</param>
    /// <param name="username">Имя пользователя.</param>
    /// <param name="password">Пароль пользователя.</param>
    /// <returns>Актуальный AccessToken или пустая строка, если не удалось получить.</returns>
    public async Task<string> GetAccessTokenAsync(string baseUrl, string username, string password)
    {
        var apiUrl = baseUrl + "/auth/access_token";
        Token? currentToken = null;

        currentToken = ReadCachedToken();

        if (currentToken != null && IsTokenActual())
        {
            this.AccessToken = currentToken.AccessToken;
            this.ExpiresIn = currentToken.ExpiresIn;
            this.TokenType = currentToken.TokenType;
            this.Scope = currentToken.Scope;
            this.RefreshToken = currentToken.RefreshToken;
            this.ExpiresAt = currentToken.ExpiresAt;
            return currentToken.AccessToken;
        }

        _logger.LogWarning("Кэшированный токен отсутствует или неактуален. Получаю новый токен...");

        currentToken = await GetTokenFromApiAsync(apiUrl, username, password);

        if (currentToken == null || string.IsNullOrEmpty(currentToken.AccessToken))
        {
            _logger.LogError("Не удалось получить новый токен из API.");
            return string.Empty;
        }

        this.AccessToken = currentToken.AccessToken;
        this.ExpiresIn = currentToken.ExpiresIn;
        this.TokenType = currentToken.TokenType;
        this.Scope = currentToken.Scope;
        this.RefreshToken = currentToken.RefreshToken;
        this.ExpiresAt = currentToken.ExpiresAt;

        CacheCurrentToken();
        return this.AccessToken;
    }
}