using MegaplanSync.ApiClient;
using MegaplanSync.Core;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.DataAccess.MySql;
using MegaplanSync.Logging;
using Microsoft.Extensions.Configuration;

namespace MegaplanSync.Service;

internal class Program
{
    static async Task Main(string[] args)
    {
        Logger.Initialize();
        ILogger logger = Logger.Instance;
        logger.OnLogFormattedMessage += Console.WriteLine;
        logger.LogInformation("Инициализация");

        IConfiguration configuration = BuildConfiguration();

        var appSettings = configuration.GetSection(AppSettings.SectionName).Get<AppSettings>();
        if (appSettings == null
            || appSettings.LaunchTime.Length == 0
            || string.IsNullOrWhiteSpace(appSettings.Username)
            || string.IsNullOrWhiteSpace(appSettings.Password)
            || string.IsNullOrWhiteSpace(appSettings.BaseApUrl)
            || string.IsNullOrWhiteSpace(appSettings.ConnectionString))
        {
            logger.LogCritical("Ошибка: некорректные настройки. Проверьте appsettings.json, user secrets и переменные окружения.");
            return;
        }
        IApiClient apiClient = new MegaApiClient(logger: logger, tokenFile: Consts.TOKEN_FILE_MEGAPLAN,
            tokenExpAtFile: Consts.TOKEN_EXP_AT_FILE_MEGAPLAN, baseApiUrl: appSettings.BaseApUrl,
            username: appSettings.Username, password: appSettings.Password);
        IApiDataMapper apiDataMapper = new ApiDataMapper(logger);
        IDataAccess dataAccess = new MySqlDataAccess(logger, appSettings.ConnectionString);
        IDbDataMapper dbDataMapper = new DbDataMapper(logger);
        ApiService apiService = new(logger, apiClient, apiDataMapper);
        DbService dbService = new(logger, dataAccess, dbDataMapper);

        while (true)
        {
            TimeSpan delay = TimeSpan.Zero;
            try
            {
                delay = CalculateDelay(appSettings.LaunchTime, logger);
                logger.LogInformation($"Программа переходит в режим ожидания. Ожидание: {delay:hh\\:mm\\:ss}");
                await Task.Delay(delay);

                logger.LogInformation("Запускаю цикл синхронизации");

                var worker = new Worker(apiService, dbService, logger);
                // не менять порядок синхронизации!
                // редко изменяющиеся сущности
                await worker.SyncDepartments();
                await worker.SyncTodoStatuses();
                await worker.SyncTodoCategories();
                await worker.SyncContractors();
                await worker.SyncEmployees();

                // часто изменяющиеся сущности
                await worker.SyncDeals();   // создает список сделок в файле
                await worker.SyncDealsStatusHistory(); // использует этот список в работе
                await worker.SyncTodos();

                // метод переехал в UpdateDealsInDb
                //await dbService.UpdateLog();

                logger.LogInformation($"Цикл синхронизации завершён.");
            }
            catch (Exception ex)
            {
                logger.LogCritical($"Работа программы завершена c ошибкой: {ex.Message}");
                // TODO: уведомление администратору
                await Task.Delay(TimeSpan.FromHours(Consts.DEFAULT_DELAY_HOURS));
            }
        }
    }

    /// <summary>
    /// Собирает IConfiguration из трех источников (в порядке возрастания приоритета):
    /// appsettings.json → user secrets → переменные окружения.
    /// </summary>
    public static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static TimeSpan CalculateDelay(string[] scheduleTimes, ILogger logger)
    {
        var times = scheduleTimes
            .Select(t => TimeSpan.TryParse(t, out var ts) ? ts : (TimeSpan?)null)
            .Where(ts => ts.HasValue)
            .Select(ts => ts.Value)
            .OrderBy(ts => ts)
            .ToList();

        DateTime now = DateTime.Now;
        TimeSpan nowTime = now.TimeOfDay;

        if (times.Count == 0)
        {
            logger.LogWarning("Расписание пустое или некорректное, перезапуск через 1 час.");
            return TimeSpan.FromHours(Consts.DEFAULT_DELAY_HOURS);
        }

        var nextToday = times.FirstOrDefault(t => t > nowTime);

        if (nextToday != default)
        {
            var nextDateTime = now.Date.Add(nextToday);
            var delay = nextDateTime - now;
            logger.LogInformation($"Следующий запуск запланирован на {nextDateTime:dd.MM.yyyy HH:mm:ss}");
            return delay;
        }
        else
        {
            var nextDateTime = now.Date.AddDays(1).Add(times.First());
            var delay = nextDateTime - now;
            logger.LogInformation($"Следующий запуск запланирован на {nextDateTime:dd.MM.yyyy HH:mm:ss}");
            return delay;
        }
    }
}
