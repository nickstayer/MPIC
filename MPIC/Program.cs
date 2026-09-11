using MegaplanSync.ApiClient;
using MegaplanSync.Core;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Logging;
using MegaplanSync.Service;
using System.Text.RegularExpressions;

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
                Console.ReadLine();
                return;
            }

            while (true)
            {
                logger.LogInformation("--- Начало цикла проверки ---");
                foreach (var mailbox in rootSettings.MonitoredMailboxes)
                {
                    await CheckMailboxIntegration(logger, emailService, notificationManager, mailbox, rootSettings);
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

        /// <summary>
        /// Извлекает чистый email-адрес из поля контактной информации Мегаплана.
        /// Мегаплан может хранить email в формате "E-mail user@domain.ru" или просто "user@domain.ru".
        /// </summary>
        private static string? ExtractEmailFromContactValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return null;

            // Ищем email в строке (на случай "E-mail user@domain.ru" или "Email: user@domain.ru")
            var match = Regex.Match(rawValue, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
            if (match.Success)
                return match.Value.ToLowerInvariant();

            // Если регулярка не нашла — возвращаем оригинал (может быть просто email)
            return rawValue.ToLowerInvariant();
        }

        private static async Task CheckMailboxIntegration(ILogger logger, EmailService emailService,
            NotificationManager notificationManager, MailboxSettings mailbox, RootSettings rootSettings)
        {
            int maxTimeToCreateDealAfterLetter = rootSettings.MaxTimeToCreateDealAfterLetter;
            bool skipReplyLetters = rootSettings.SkipReplyLetters;
            string mailboxId = mailbox.Username;

            logger.LogInformation($"--- Проверка интеграции для ящика {mailboxId} ---");

            // Получаем время последнего обработанного письма (UTC)
            var lastProcessedUtc = notificationManager.GetLastProcessedTimeUtc(mailboxId);

            // Получаем новые письма (после lastProcessedUtc) или последние 10, если ящик ещё не проверялся
            var emails = await GetRecentLetters(logger, mailbox.Username, mailbox.Password, lastProcessedUtc);
            if (emails == null || emails.Count == 0)
            {
                logger.LogInformation($"Новых писем для {mailboxId} с {lastProcessedUtc:yyyy-MM-dd HH:mm:ss} UTC не найдено.");
                return;
            }

            var newestEmail = emails.First();
            var oldestEmailInBatch = emails.Last();
            logger.LogInformation($"Получено {emails.Count} писем. Самое новое: от {newestEmail.Sender} " +
                $"в {newestEmail.ReceivedDate:yyyy-MM-dd HH:mm:ss zzz}. Самое старое: от {oldestEmailInBatch.Sender} " +
                $"в {oldestEmailInBatch.ReceivedDate:yyyy-MM-dd HH:mm:ss zzz}.");

            // Запрашиваем сделки с запасом, чтобы гарантированно получить все, созданные от этих писем
            var oldestEmailUtc = oldestEmailInBatch.ReceivedDate.ToUniversalTime().DateTime;
            var apiQueryTime = oldestEmailUtc.AddHours(-1);

            var lastDeals = await GetLastDeals(logger, apiQueryTime);
            if (lastDeals == null)
            {
                string errorMsg = $"Не удалось получить сделки из Мегаплана для проверки ящика {mailboxId}.";
                logger.LogError(errorMsg, logToConsole: true);
                await emailService.SendNotificationAsync($"Ошибка интеграции MPIC: {mailboxId}", errorMsg);
                return;
            }

            // Перебираем письма от самого нового к самому старому.
            // Если порог для письма ещё не истёк — прекращаем проверку (это и все более старые будут на следующем цикле).
            int processedCount = 0;
            var nowUtc = DateTime.UtcNow;

            foreach (var email in emails)
            {
                var emailReceivedUtc = email.ReceivedDate.ToUniversalTime().DateTime;
                var elapsedMinutes = (nowUtc - emailReceivedUtc).TotalMinutes;

                // Пропускаем письма-ответы/пересылки, если включена опция
                if (skipReplyLetters && email.IsReplyOrForward())
                {
                    logger.LogInformation($"Пропущено письмо-ответ/пересылка от {email.Sender} (тема: \"{email.Subject ?? "(без темы)"}\").");
                    notificationManager.UpdateLastProcessedTimeUtc(mailboxId, emailReceivedUtc);
                    processedCount++;
                    continue;
                }

                // Если прошло меньше порога — не обрабатываем это и все более старые письма
                if (elapsedMinutes < maxTimeToCreateDealAfterLetter)
                {
                    logger.LogInformation($"Письмо от {email.Sender} от {emailReceivedUtc:yyyy-MM-dd HH:mm:ss} UTC: " +
                        $"прошло {elapsedMinutes:F1} мин, порог {maxTimeToCreateDealAfterLetter} мин. Ожидаем до следующего цикла.");
                    break;
                }

                // Порог истёк — проверяем, есть ли сделка, созданная ПОСЛЕ этого письма от этого же отправителя
                bool isDealCreated = false;
                bool isWarning = false;
                string resultMessage = "";

                // Ищем сделки, email контрагента которых совпадает с отправителем письма
                var senderEmailLower = email.Sender?.ToLowerInvariant();
                var matchingDeals = lastDeals
                    .Where(d => d?.TimeCreated?.Value != null && d.Contractor?.ContactInfo != null && senderEmailLower != null)
                    .Select(d => new
                    {
                        Deal = d,
                        ContactEmails = d.Contractor.ContactInfo
                            .Select(ci => ExtractEmailFromContactValue(ci.Value))
                            .Where(e => e != null)
                            .ToList()
                    })
                    .Where(x => x.ContactEmails.Contains(senderEmailLower!))
                    .Select(x => x.Deal)
                    // ТОЛЬКО сделки, созданные СТРОГО ПОСЛЕ письма
                    .Where(d => d.TimeCreated!.Value > emailReceivedUtc)
                    // Сортируем по времени создания (самая ранняя созданная после письма)
                    .OrderBy(d => d.TimeCreated!.Value)
                    .ToList();

                if (matchingDeals.Any())
                {
                    var bestMatchDeal = matchingDeals.First();
                    var dealTimeCreated = bestMatchDeal.TimeCreated!.Value;
                    var diffMinutes = (dealTimeCreated - emailReceivedUtc).TotalMinutes;

                    // diffMinutes гарантированно >= 0, т.к. фильтр выше
                    if (diffMinutes < maxTimeToCreateDealAfterLetter)
                    {
                        // УСПЕХ — сделка создана в пределах порога
                        isDealCreated = true;
                        resultMessage = $"Интеграция с ящиком {mailboxId} работает исправно." +
                            $" Письмо от {email.Sender} получено в {emailReceivedUtc:yyyy-MM-dd HH:mm:ss} UTC." +
                            $" Сделка создана в {dealTimeCreated:yyyy-MM-dd HH:mm:ss}, через {diffMinutes:F2} мин.";
                        logger.LogInformation(resultMessage);

                        if (notificationManager.ShouldSendSuccessNotification(mailboxId))
                        {
                            logger.LogInformation("Обнаружено восстановление работы интеграции. Отправка уведомления.");
                            await emailService.SendNotificationAsync($"Восстановление интеграции MPIC: {mailboxId}",
                                $"Интеграция восстановлена. " +
                                $"Последняя успешная сделка создана для письма от {email.Sender} в {dealTimeCreated:yyyy-MM-dd HH:mm:ss}.");
                            notificationManager.RecordSuccess(mailboxId);
                        }
                    }
                    else
                    {
                        // СДЕЛКА НАЙДЕНА, НО С ЗАДЕРЖКОЙ — предупреждение
                        isWarning = true;
                        resultMessage = $"Интеграция с ящиком {mailboxId} работает, но на создание сделки ушло {diffMinutes:F2} мин" +
                            $" (больше порога в {maxTimeToCreateDealAfterLetter} мин)." +
                            $" Письмо от {email.Sender} от {emailReceivedUtc:yyyy-MM-dd HH:mm:ss} UTC.";
                        logger.LogWarning(resultMessage);

                        if (notificationManager.ShouldSendFailureNotification(mailboxId, email.MessageId, email.Sender, emailReceivedUtc))
                        {
                            await emailService.SendNotificationAsync($"Предупреждение интеграции MPIC: {mailboxId}", resultMessage);
                            notificationManager.RecordFailure(mailboxId, email.MessageId, email.Sender, emailReceivedUtc);
                        }
                    }
                }

                if (!isDealCreated && !isWarning)
                {
                    // СДЕЛКА НЕ НАЙДЕНА — сбой интеграции
                    resultMessage = $"ИНТЕГРАЦИЯ НЕ РАБОТАЕТ: Для ящика {mailboxId} не найдено ни одной сделки, " +
                        $"созданной после письма от <b>{email.Sender}</b>, полученного в {emailReceivedUtc:yyyy-MM-dd HH:mm:ss} UTC.";
                    logger.LogError(resultMessage, logToConsole: true);

                    if (notificationManager.ShouldSendFailureNotification(mailboxId, email.MessageId, email.Sender, emailReceivedUtc))
                    {
                        await emailService.SendNotificationAsync($"Сбой интеграции MPIC: {mailboxId}", resultMessage);
                        notificationManager.RecordFailure(mailboxId, email.MessageId, email.Sender, emailReceivedUtc);
                    }
                }

                // Помечаем письмо как обработанное
                notificationManager.UpdateLastProcessedTimeUtc(mailboxId, emailReceivedUtc);
                processedCount++;
            }

            if (processedCount > 0)
            {
                logger.LogInformation($"Писем обработано: {processedCount}");
            }
        }

        /// <summary>
        /// Получает последние письма из ящика, начиная с указанной даты (UTC).
        /// Если дата минимальная — берёт последние 10 писем.
        /// </summary>
        private static async Task<List<EmailDetails>?> GetRecentLetters(ILogger logger, string username,
            string password, DateTime lastProcessedUtc)
        {
            var emailReader = new EmailReader(username, password);
            var emailDetailsList = await emailReader.GetRecentEmailsAsync(lastProcessedUtc, count: 10);

            if (emailDetailsList == null || emailDetailsList.Count == 0)
            {
                logger.LogInformation("Новых писем не найдено.");
                return null;
            }

            logger.LogInformation($"Получено писем: {emailDetailsList.Count}. " +
                $"Самое новое: {emailDetailsList.First().ReceivedDate:yyyy-MM-dd HH:mm:ss zzz}");

            return emailDetailsList;
        }

        private static async Task<List<Deal>> GetLastDeals(ILogger logger, DateTime targetDateTimeUtc)
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
            ApiService apiService = new(logger, apiClient, apiDataMapper);

            List<Deal> deals = await apiService.GetAndMapDealsUpdatedAfter(targetDateTimeUtc);
            return deals;
        }
    }
}