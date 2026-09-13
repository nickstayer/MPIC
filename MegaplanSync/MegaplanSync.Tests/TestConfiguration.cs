using MegaplanSync.Core;
using Microsoft.Extensions.Configuration;

namespace MegaplanSync.Tests;

/// <summary>
/// Конфигурация для тестов. Собирается из тех же трех источников,
/// что и рабочие приложения (в порядке возрастания приоритета):
/// appsettings.json (в каталоге тестов) → user secrets → переменные окружения.
/// </summary>
public class TestConfiguration
{
    private static readonly Lazy<IConfiguration> _lazyConfiguration = new(BuildConfiguration);

    public static IConfiguration Configuration => _lazyConfiguration.Value;

    public static AppSettings GetAppSettings() =>
        Configuration.GetSection(AppSettings.SectionName).Get<AppSettings>() ?? new AppSettings();

    /// <summary>
    /// Строка подключения по имени из секции MegaplanSync:ConnectionStrings (например "Test").
    /// </summary>
    public static string GetConnectionString(string name) =>
        Configuration.GetSection(AppSettings.SectionName)["ConnectionStrings:" + name] ?? string.Empty;

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets<TestConfiguration>(optional: true)
            .AddEnvironmentVariables()
            .Build();
    }
}
