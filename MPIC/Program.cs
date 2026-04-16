using MegaplanSync.ApiClient;
using MegaplanSync.Core;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Logging;
using MegaplanSync.Service;


namespace MPIC
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Logger.Initialize();
            ILogger logger = Logger.Instance;
            logger.OnLogFormattedMessage += Console.WriteLine;
            logger.LogInformation("Инициализация");

            var serializer = new JsonHelper(logger);
            var rootSettings = serializer.LoadEntityFromFile<RootSettings>(Consts.APP_SETTINGS_FILE);

            var emailService = new EmailService(rootSettings?.NotificationSettings, logger);
            var notificationManager = new NotificationManager();

            if (rootSettings == null || rootSettings.MonitoredMailboxes == null || rootSettings.MonitoredMailboxes.Count == 0)
            {
                logger.LogCritical("Ошибка: настройки мониторинга не найдены или пусты в appsettings.json.");
                await emailService.SendNotificationAsync("Критическая ошибка MPIC", "Ошибка: настройки мониторинга не найдены или пусты в appsettings.json.");
                Console.ReadLine(); // Keep readline for critical error exit
                return;
            }

            while (true)
            {
                logger.LogInformation("--- Начало цикла проверки ---");
                foreach (var mailbox in rootSettings.MonitoredMailboxes)
                {
                    await CheckMailboxIntegration(logger, emailService, notificationManager, mailbox, rootSettings.MaxTimeToCreateDealAfterLetter);
                }
                logger.LogInformation("--- Конец цикла проверки ---");

                if (rootSettings.RunIntervalMinutes > 0)
                {
                    logger.LogInformation($"Следующий запуск через {rootSettings.RunIntervalMinutes} мин.");
                    await Task.Delay(rootSettings.RunIntervalMinutes * 60 * 1000);
                }
                else
                {
                    logger.LogInformation("Интервал запуска не настроен или равен 0. Завершение работы.");
                    break;
                }
            }
        }

        private static async Task CheckMailboxIntegration(ILogger logger, EmailService emailService, NotificationManager notificationManager, MailboxSettings mailbox, int maxTimeToCreateDealAfterLetter)
        {
            logger.LogInformation($"--- Проверка интеграции для ящика {mailbox.Username} ---");

            var lastLetter = await GetLastLetterInfo(logger, mailbox.Username, mailbox.Password);
            if (lastLetter == null)
            {
                string errorMsg = $"Не удалось получить последнее письмо для {mailbox.Username}.";
                logger.LogError(errorMsg, logToConsole: true);
                // This is a system-level error, not a logic failure, so we'll let it notify every time.
                await emailService.SendNotificationAsync($"Ошибка интеграции MPIC: {mailbox.Username}", errorMsg);
                return;
            }
            
            if (lastLetter.MessageId == null)
            {
                logger.LogWarning($"Не удалось получить Message-ID для последнего письма. Уведомления для этого письма не будут отслеживаться.");
            }

            var targetDateTime = lastLetter.ReceivedDate.ToLocalTime().DateTime;
            var lastDeals = await GetLastDeals(logger, targetDateTime);
            if (lastDeals == null)
            {
                 string errorMsg = $"Не удалось получить сделки из Мегаплана для проверки ящика {mailbox.Username}.";
                 logger.LogError(errorMsg, logToConsole: true);
                 // This is also a system-level error.
                 await emailService.SendNotificationAsync($"Ошибка интеграции MPIC: {mailbox.Username}", errorMsg);
                 return;
            }

            var contractorEmailInLastDeals = lastDeals
                .Where(d => d?.Contractor?.ContactInfo != null)
                .SelectMany(d => d.Contractor.ContactInfo)
                .Where(ci => !string.IsNullOrEmpty(ci?.Value) && ci.Value.Contains('@'))
                .Select(ci => ci.Value)
                .ToList();

            var match = contractorEmailInLastDeals.Where(d => d == lastLetter.Sender).ToList();

            if (match.Any())
            {
                // Находим все сделки от нужного отправителя
                var dealsFromSender = lastDeals.Where(d =>
                    d.Contractor?.ContactInfo?.Any(ci => ci.Value == lastLetter.Sender) ?? false);

                // Из них выбираем самую раннюю, созданную после письма
                var bestMatchDeal = dealsFromSender
                    .Where(d => d.TimeCreated != null)
                    .OrderBy(d => d.TimeCreated.Value)
                    .FirstOrDefault();

                if (bestMatchDeal != null)
                {
                    var dealTimeCreated = bestMatchDeal.TimeCreated.Value;
                    var diff = (dealTimeCreated - targetDateTime).TotalMinutes;

                    if (diff < maxTimeToCreateDealAfterLetter)
                    {
                        string successMsg = $"Интеграция с ящиком {mailbox.Username} работает исправно. Письмо получено в {targetDateTime}. Сделка создана в {dealTimeCreated}, через {diff:F2} мин.";
                        logger.LogInformation(successMsg);
                        
                        if (notificationManager.ShouldSendSuccessNotification(mailbox.Username))
                        {
                            logger.LogInformation("Обнаружено восстановление работы интеграции. Отправка уведомления.");
                            await emailService.SendNotificationAsync($"Восстановление интеграции MPIC: {mailbox.Username}", $"Интеграция восстановлена. Последняя успешная сделка создана для письма от {lastLetter.Sender} в {dealTimeCreated}.");
                            notificationManager.RecordSuccess(mailbox.Username);
                        }
                    }
                    else
                    {
                        string warningMsg = $"Интеграция с ящиком {mailbox.Username} работает, но на создание сделки ушло {diff:F2} минут (больше порога в {maxTimeToCreateDealAfterLetter} мин).";
                        logger.LogWarning(warningMsg);
                        // Warnings are treated as failures for notification logic to indicate a problem.
                        if (notificationManager.ShouldSendFailureNotification(mailbox.Username, lastLetter.MessageId))
                        {
                           await emailService.SendNotificationAsync($"Предупреждение интеграции MPIC: {mailbox.Username}", warningMsg);
                           notificationManager.RecordFailure(mailbox.Username, lastLetter.MessageId);
                        }
                    }
                }
                else
                {
                    // Этот 'else' соответствует 'if (bestMatchDeal != null)'
                    string errorMsg = $"ИНТЕГРАЦИЯ НЕ РАБОТАЕТ: Для ящика {mailbox.Username} не найдено ни одной сделки от {lastLetter.Sender}, созданной после письма, полученного в {targetDateTime}.";
                    logger.LogError(errorMsg, logToConsole: true);
                    if (notificationManager.ShouldSendFailureNotification(mailbox.Username, lastLetter.MessageId))
                    {
                        await emailService.SendNotificationAsync($"Сбой интеграции MPIC: {mailbox.Username}", errorMsg);
                        notificationManager.RecordFailure(mailbox.Username, lastLetter.MessageId);

                    }
                }
            }
            else
            {
                string errorMsg = $"ИНТЕГРАЦИЯ НЕ РАБОТАЕТ: Для ящика {mailbox.Username} не найдено ни одной сделки, созданной после письма от <b>{lastLetter.Sender}</b>, полученного в {targetDateTime}.";
                logger.LogError(errorMsg, logToConsole: true);
                if (notificationManager.ShouldSendFailureNotification(mailbox.Username, lastLetter.MessageId))
                {
                    await emailService.SendNotificationAsync($"Сбой интеграции MPIC: {mailbox.Username}", errorMsg);
                    notificationManager.RecordFailure(mailbox.Username, lastLetter.MessageId);
                }
            }
        }


        private static async Task<List<Deal>> GetLastDeals(ILogger logger, DateTime targetDateTime)
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
            IApiClient apiClient = new MegaApiClient(logger: logger, tokenFile: Consts.TOKEN_FILE_MEGAPLAN,
                tokenExpAtFile: Consts.TOKEN_EXP_AT_FILE_MEGAPLAN, baseApiUrl: appSettings.BaseApUrl,
                username: appSettings.Username, password: appSettings.Password);
            IApiDataMapper apiDataMapper = new ApiDataMapper(logger);
            IDbDataMapper dbDataMapper = new DbDataMapper(logger);
            ApiService apiService = new(logger, apiClient, apiDataMapper);

            List<Deal> deals = await apiService.GetAndMapDealsUpdatedAfter(targetDateTime);
            return deals;
        }

        private static async Task<EmailDetails> GetLastLetterInfo(ILogger logger, string username, string password)
        {

            var emailReader = new EmailReader(username, password);
            var emailDetails = await emailReader.GetLastEmailDetailsAsync();

            if (emailDetails != null)
            {
                logger.LogInformation("Данные о последнем письме получены");
                return emailDetails;
            }
            else
            {
                logger.LogError("Не удалось получить данные о последнем письме", logToConsole: true);
                return null;
            }
        }
    }
}
