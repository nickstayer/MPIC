using MegaplanSync.ApiClient;
using MegaplanSync.Core;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MPIC;

public class MpicWorker : BackgroundService
{
    private readonly ILogger<MpicWorker> _logger;
    private readonly RootSettings _settings;

    public MpicWorker(ILogger<MpicWorker> logger, IOptions<RootSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Инициализация службы MPIC");

        MegaplanSync.Core.Interfaces.ILogger logger = Logger.Instance;
        logger.LogInformation("Инициализация");

        var emailService = new EmailService(_settings.NotificationSettings, logger);
        var notificationManager = new NotificationManager();

        if (_settings.MonitoredMailboxes.Count == 0)
        {
            logger.LogCritical("❌Ошибка: настройки мониторинга не найдены или пусты. Проверьте appsettings.json, user secrets и переменные окружения.");
            await emailService.SendNotificationAsync(
                "Критическая ошибка MPIC",
                "Ошибка: настройки мониторинга не найдены или пусты. Проверьте appsettings.json, user secrets и переменные окружения.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("--- Начало цикла проверки ---");

            foreach (var mailbox in _settings.MonitoredMailboxes)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                await CheckMailboxIntegration(
                    logger, emailService, notificationManager, mailbox,
                    _settings, stoppingToken);
            }

            logger.LogInformation("--- Конец цикла проверки ---");

            if (_settings.RunIntervalMinutes > 0)
            {
                logger.LogInformation($"Следующий запуск через {_settings.RunIntervalMinutes} мин.");
                try
                {
                    await Task.Delay(
                        _settings.RunIntervalMinutes * 60 * 1000,
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
                logger.LogInformation("❌Интервал запуска не настроен или равен 0. Завершение работы.");
                break;
            }
        }

        logger.LogInformation("❌Служба MPIC остановлена.");
    }

    private static async Task CheckMailboxIntegration(
        MegaplanSync.Core.Interfaces.ILogger logger,
        EmailService emailService,
        NotificationManager notificationManager,
        MailboxSettings mailbox,
        RootSettings settings,
        CancellationToken stoppingToken)
    {
        int maxTimeToCreateDealAfterLetter = settings.MaxTimeToCreateDealAfterLetter;
        logger.LogInformation($"--- Проверка интеграции для ящика {mailbox.Username} ---");

        var lastLetter = await GetLastLetterInfo(logger, mailbox.Username, mailbox.Password, settings.Imap);
        if (lastLetter == null)
        {
            string errorMsg = $"❌Не удалось получить последнее письмо для {mailbox.Username}.";
            logger.LogError(errorMsg, logToConsole: true);
            await emailService.SendNotificationAsync(
                $"Ошибка интеграции MPIC: {mailbox.Username}", errorMsg);
            return;
        }

        if (lastLetter.MessageId == null)
        {
            logger.LogWarning(
                "⚠️Не удалось получить Message-ID для последнего письма. " +
                "Уведомления для этого письма не будут отслеживаться.");
        }

        var targetDateTime = lastLetter.ReceivedDate.ToLocalTime().DateTime;
        logger.LogInformation($"Работаю с письмом от {lastLetter.Sender}, получено: {targetDateTime}");

        // Внутренний цикл ожидания вместо рекурсии — корректно реагирует на остановку службы
        while (!stoppingToken.IsCancellationRequested)
        {
            var lastDeals = await GetLastDeals(logger, settings, targetDateTime);
            if (lastDeals == null)
            {
                string errorMsg =
                    $"❌Не удалось получить сделки из Мегаплана для проверки ящика {mailbox.Username}.";
                logger.LogError(errorMsg, logToConsole: true);
                await emailService.SendNotificationAsync(
                    $"Ошибка интеграции MPIC: {mailbox.Username}", errorMsg);
                return;
            }

            bool isDealCreated = await TryHandleDealFound(
                logger, emailService, notificationManager,
                mailbox, lastLetter, lastDeals,
                targetDateTime, maxTimeToCreateDealAfterLetter);

            if (isDealCreated)
            {
                logger.LogInformation("✅Сделка из письма была создана.");
                return;
            }
                

            var timeSinceLetter = (DateTime.Now - targetDateTime).TotalMinutes;

            if (timeSinceLetter < maxTimeToCreateDealAfterLetter)
            {
                var timeToWaitMinutes = maxTimeToCreateDealAfterLetter - timeSinceLetter;
                logger.LogInformation(
                    $"⚠️Сделка для ящика {mailbox.Username} еще не создана. " +
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
                $"❌Сделка не была создана. Источник {mailbox.Username}, " +
                $"отправитель: <b>{lastLetter.Sender}</b>, получено в {targetDateTime}.";
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
    private static async Task<bool> TryHandleDealFound(
        MegaplanSync.Core.Interfaces.ILogger logger,
        EmailService emailService,
        NotificationManager notificationManager,
        MailboxSettings mailbox,
        EmailDetails lastLetter,
        List<Deal> lastDeals,
        DateTime targetDateTime,
        int maxTimeToCreateDealAfterLetter)
    {
        var contractorEmailsInLastDeals = lastDeals
            .Where(d => d?.Contractor?.ContactInfo != null)
            .SelectMany(d => d.Contractor!.ContactInfo!)
            .Where(ci => !string.IsNullOrEmpty(ci?.Value) && ci.Value.Contains('@'))
            .Select(ci => ci.Value!)
            .ToList();

        if (lastLetter.Sender == null
            || !contractorEmailsInLastDeals.Contains(lastLetter.Sender))
            return false;

        var dealsFromSender = lastDeals.Where(d =>
            d.Contractor?.ContactInfo?.Any(ci => ci.Value == lastLetter.Sender) ?? false);

        var bestMatchDeal = dealsFromSender
            .Where(d => d.TimeCreated != null)
            .OrderBy(d => d.TimeCreated!.Value)
            .FirstOrDefault();

        if (bestMatchDeal == null)
            return false;

        return true;
    }

    private static async Task<List<Deal>?> GetLastDeals(
        MegaplanSync.Core.Interfaces.ILogger logger,
        RootSettings settings,
        DateTime targetDateTime)
    {
        if (settings == null
            || string.IsNullOrWhiteSpace(settings.Username)
            || string.IsNullOrWhiteSpace(settings.Password)
            || string.IsNullOrWhiteSpace(settings.BaseApUrl))
        {
            logger.LogCritical("❌Ошибка: некорректные настройки для доступа к API Мегаплана.");
            return null;
        }

        IApiClient apiClient = new MegaApiClient(
            logger: logger,
            tokenFile: Consts.TOKEN_FILE_MEGAPLAN,
            tokenExpAtFile: Consts.TOKEN_EXP_AT_FILE_MEGAPLAN,
            baseApiUrl: settings.BaseApUrl,
            username: settings.Username,
            password: settings.Password);

        IApiDataMapper apiDataMapper = new ApiDataMapper(logger);
        ApiService apiService = new(logger, apiClient, apiDataMapper);

        List<Deal> deals = await apiService.GetAndMapDealsUpdatedAfter(targetDateTime);
        return deals;
    }

    private static async Task<EmailDetails?> GetLastLetterInfo(
        MegaplanSync.Core.Interfaces.ILogger logger, string username, string password, ImapSettings imapSettings)
    {
        var emailReader = new EmailReader(username, password, imapSettings);
        var emailDetails = await emailReader.GetLastEmailDetailsAsync();

        if (emailDetails != null)
        {
            logger.LogInformation("Данные о последнем письме получены");
            return emailDetails;
        }

        logger.LogError("❌Не удалось получить данные о последнем письме", logToConsole: true);
        return null;
    }
}
