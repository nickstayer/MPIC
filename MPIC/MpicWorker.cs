using MegaplanSync.ApiClient;
using MegaplanSync.Core;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Logging;
using MegaplanSync.Service;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MPIC;

public class MpicWorker : BackgroundService
{
    private readonly ILogger<MpicWorker> _logger;

    public MpicWorker(ILogger<MpicWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Инициализация службы MPIC");

        // ВНИМАНИЕ: здесь используется ваш собственный ILogger (MegaplanSync.Core.Interfaces..ILogger)
        // для сохранения существующей логики. Стандартный Microsoft.Extensions.Logging.ILogger
        // используется только для сообщений о жизненном цикле службы.
        MegaplanSync.Core.Interfaces.ILogger logger = Logger.Instance;
        logger.LogInformation("Инициализация");

        var serializer = new JsonHelper(logger);
        var rootSettings = serializer.LoadEntityFromFile<RootSettings>(Consts.APP_SETTINGS_FILE);

        var emailService = new EmailService(rootSettings?.NotificationSettings, logger);
        var notificationManager = new NotificationManager();

        if (rootSettings == null
            || rootSettings.MonitoredMailboxes == null
            || rootSettings.MonitoredMailboxes.Count == 0)
        {
            logger.LogCritical("Ошибка: настройки мониторинга не найдены или пусты в appsettings.json.");
            await emailService.SendNotificationAsync(
                "Критическая ошибка MPIC",
                "Ошибка: настройки мониторинга не найдены или пусты в appsettings.json.");

            // В службе нельзя ждать ввода — просто завершаем работу.
            // Хост остановится, systemd/SCM зафиксируют завершение.
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("--- Начало цикла проверки ---");

            foreach (var mailbox in rootSettings.MonitoredMailboxes)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                await CheckMailboxIntegration(
                    logger, emailService, notificationManager, mailbox,
                    rootSettings.MaxTimeToCreateDealAfterLetter,
                    stoppingToken);
            }

            logger.LogInformation("--- Конец цикла проверки ---");

            if (rootSettings.RunIntervalMinutes > 0)
            {
                logger.LogInformation($"Следующий запуск через {rootSettings.RunIntervalMinutes} мин.");
                try
                {
                    await Task.Delay(
                        rootSettings.RunIntervalMinutes * 60 * 1000,
                        stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Ожидаемое поведение при остановке службы
                    break;
                }
            }
            else
            {
                logger.LogInformation("Интервал запуска не настроен или равен 0. Завершение работы.");
                break;
            }
        }

        logger.LogInformation("Служба MPIC остановлена.");
    }

    private static async Task CheckMailboxIntegration(
        MegaplanSync.Core.Interfaces.ILogger logger,
        EmailService emailService,
        NotificationManager notificationManager,
        MailboxSettings mailbox,
        int maxTimeToCreateDealAfterLetter,
        CancellationToken stoppingToken)
    {
        logger.LogInformation($"--- Проверка интеграции для ящика {mailbox.Username} ---");

        var lastLetter = await GetLastLetterInfo(logger, mailbox.Username, mailbox.Password);
        if (lastLetter == null)
        {
            string errorMsg = $"Не удалось получить последнее письмо для {mailbox.Username}.";
            logger.LogError(errorMsg, logToConsole: true);
            await emailService.SendNotificationAsync(
                $"Ошибка интеграции MPIC: {mailbox.Username}", errorMsg);
            return;
        }

        if (lastLetter.MessageId == null)
        {
            logger.LogWarning(
                "Не удалось получить Message-ID для последнего письма. " +
                "Уведомления для этого письма не будут отслеживаться.");
        }

        var targetDateTime = lastLetter.ReceivedDate.ToLocalTime().DateTime;

        // Внутренний цикл ожидания вместо рекурсии — корректно реагирует на остановку службы
        while (!stoppingToken.IsCancellationRequested)
        {
            var lastDeals = await GetLastDeals(logger, targetDateTime);
            if (lastDeals == null)
            {
                string errorMsg =
                    $"Не удалось получить сделки из Мегаплана для проверки ящика {mailbox.Username}.";
                logger.LogError(errorMsg, logToConsole: true);
                await emailService.SendNotificationAsync(
                    $"Ошибка интеграции MPIC: {mailbox.Username}", errorMsg);
                return;
            }

            bool isDealCreated = TryHandleDealFound(
                logger, emailService, notificationManager,
                mailbox, lastLetter, lastDeals,
                targetDateTime, maxTimeToCreateDealAfterLetter);

            if (isDealCreated)
                return;

            var timeSinceLetter = (DateTime.Now - targetDateTime).TotalMinutes;

            if (timeSinceLetter < maxTimeToCreateDealAfterLetter)
            {
                var timeToWaitMinutes = maxTimeToCreateDealAfterLetter - timeSinceLetter;
                logger.LogInformation(
                    $"Сделка для ящика {mailbox.Username} еще не создана. " +
                    $"Ожидание {timeToWaitMinutes:F2} мин до повторной проверки.");

                var actualWaitTime = TimeSpan.FromMinutes(Math.Max(1, timeToWaitMinutes));
                try
                {
                    await Task.Delay(actualWaitTime, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                continue; // повторная проверка того же ящика
            }

            // Время вышло, сделки нет — фиксируем сбой
            string failMsg =
                $"ИНТЕГРАЦИЯ НЕ РАБОТАЕТ: Для ящика {mailbox.Username} " +
                $"не найдено ни одной сделки, созданной после письма " +
                $"от <b>{lastLetter.Sender}</b>, полученного в {targetDateTime}.";
            logger.LogError(failMsg, logToConsole: true);

            if (notificationManager.ShouldSendFailureNotification(
                    mailbox.Username, lastLetter.MessageId))
            {
                await emailService.SendNotificationAsync(
                    $"Сбой интеграции MPIC: {mailbox.Username}", failMsg);
                notificationManager.RecordFailure(
                    mailbox.Username, lastLetter.MessageId);
            }

            return;
        }
    }

    /// <summary>
    /// Пытается найти подходящую сделку и обработать успех/предупреждение.
    /// Возвращает true, если сделка найдена и обработана (успех или слишком долгое создание).
    /// </summary>
    private static bool TryHandleDealFound(
        MegaplanSync.Core.Interfaces.ILogger logger,
        EmailService emailService,
        NotificationManager notificationManager,
        MailboxSettings mailbox,
        EmailDetails lastLetter,
        List<Deal> lastDeals,
        DateTime targetDateTime,
        int maxTimeToCreateDealAfterLetter)
    {
        var contractorEmailInLastDeals = lastDeals
            .Where(d => d?.Contractor?.ContactInfo != null)
            .SelectMany(d => d.Contractor.ContactInfo)
            .Where(ci => !string.IsNullOrEmpty(ci?.Value) && ci.Value.Contains('@'))
            .Select(ci => ci.Value)
            .ToList();

        var match = contractorEmailInLastDeals
            .Where(d => d == lastLetter.Sender)
            .ToList();

        if (!match.Any())
            return false;

        var dealsFromSender = lastDeals.Where(d =>
            d.Contractor?.ContactInfo?.Any(ci => ci.Value == lastLetter.Sender) ?? false);

        var bestMatchDeal = dealsFromSender
            .Where(d => d.TimeCreated != null)
            .OrderBy(d => d.TimeCreated.Value)
            .FirstOrDefault();

        if (bestMatchDeal == null)
            return false;

        var dealTimeCreated = bestMatchDeal.TimeCreated.Value;
        var diff = (dealTimeCreated - targetDateTime).TotalMinutes;

        if (diff < maxTimeToCreateDealAfterLetter)
        {
            string successMsg =
                $"Интеграция с ящиком {mailbox.Username} работает исправно. " +
                $"Письмо получено в {targetDateTime}. Сделка создана в {dealTimeCreated}, " +
                $"через {diff:F2} мин.";
            logger.LogInformation(successMsg);

            if (notificationManager.ShouldSendSuccessNotification(mailbox.Username))
            {
                logger.LogInformation(
                    "Обнаружено восстановление работы интеграции. Отправка уведомления.");
                // Fire-and-forget здесь неуместен, но метод синхронный по сигнатуре.
                // Если нужно — сделайте TryHandleDealFound async и await здесь.
                emailService.SendNotificationAsync(
                    $"Восстановление интеграции MPIC: {mailbox.Username}",
                    $"Интеграция восстановлена. Последняя успешная сделка создана " +
                    $"для письма от {lastLetter.Sender} в {dealTimeCreated}.")
                    .GetAwaiter().GetResult();
                notificationManager.RecordSuccess(mailbox.Username);
            }

            return true;
        }

        // Сделка создана, но слишком поздно — трактуем как сбой
        string warningMsg =
            $"Интеграция с ящиком {mailbox.Username} работает, но на создание сделки " +
            $"ушло {diff:F2} минут (больше порога в {maxTimeToCreateDealAfterLetter} мин).";
        logger.LogWarning(warningMsg);

        if (notificationManager.ShouldSendFailureNotification(
                mailbox.Username, lastLetter.MessageId))
        {
            emailService.SendNotificationAsync(
                $"Предупреждение интеграции MPIC: {mailbox.Username}", warningMsg)
                .GetAwaiter().GetResult();
            notificationManager.RecordFailure(
                mailbox.Username, lastLetter.MessageId);
        }

        return true;
    }

    private static async Task<List<Deal>> GetLastDeals(
        MegaplanSync.Core.Interfaces.ILogger logger, DateTime targetDateTime)
    {
        var serializer = new JsonHelper(logger);
        var appSettings = serializer.LoadEntityFromFile<AppSettings>(Consts.APP_SETTINGS_FILE);

        if (appSettings?.LaunchTime == null
            || appSettings.LaunchTime.Length == 0
            || string.IsNullOrWhiteSpace(appSettings.Username)
            || string.IsNullOrWhiteSpace(appSettings.Password)
            || string.IsNullOrWhiteSpace(appSettings.BaseApUrl)
            || string.IsNullOrWhiteSpace(appSettings.ConnectionString))
        {
            logger.LogCritical("Ошибка: некорректные настройки для доступа к API Мегаплана.");
            return null;
        }

        IApiClient apiClient = new MegaApiClient(
            logger: logger,
            tokenFile: Consts.TOKEN_FILE_MEGAPLAN,
            tokenExpAtFile: Consts.TOKEN_EXP_AT_FILE_MEGAPLAN,
            baseApiUrl: appSettings.BaseApUrl,
            username: appSettings.Username,
            password: appSettings.Password);

        IApiDataMapper apiDataMapper = new ApiDataMapper(logger);
        IDbDataMapper dbDataMapper = new DbDataMapper(logger);
        ApiService apiService = new(logger, apiClient, apiDataMapper);

        List<Deal> deals = await apiService.GetAndMapDealsUpdatedAfter(targetDateTime);
        return deals;
    }

    private static async Task<EmailDetails> GetLastLetterInfo(
        MegaplanSync.Core.Interfaces.ILogger logger, string username, string password)
    {
        var emailReader = new EmailReader(username, password);
        var emailDetails = await emailReader.GetLastEmailDetailsAsync();

        if (emailDetails != null)
        {
            logger.LogInformation("Данные о последнем письме получены");
            return emailDetails;
        }

        logger.LogError("Не удалось получить данные о последнем письме", logToConsole: true);
        return null;
    }
}