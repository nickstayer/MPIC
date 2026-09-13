namespace MegaplanSync.Core;

/// <summary>
/// Настройки синхронизации с API Мегаплана.
/// Читаются через IConfiguration из секции <see cref="SectionName"/>.
/// Источники: appsettings.json → user secrets → переменные окружения.
/// </summary>
public class AppSettings
{
    public const string SectionName = "MegaplanSync";

    public string[] LaunchTime { get; set; } = [];
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BaseApUrl { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}
