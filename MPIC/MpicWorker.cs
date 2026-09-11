using MegaplanSync.ApiClient;
using MegaplanSync.Core;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Logging;
using MegaplanSync.Service;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace MPIC
{
    /// <summary>
    /// Фоновый сервис для мониторинга интеграции email → CRM (Мегаплан).
    /// Содержит основной цикл проверки, вынесенный из Program.cs.
    /// Настройки получает из DI через IOptions&lt;RootSettings&gt;.
    /// </summary>
    public class MpicWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly NotificationManager _notificationManager;
        private readonly RootSettings _settings;

        public MpicWorker(NotificationManager notificationManager, IOptions<RootSettings> options)
        {
            Logger.Initialize();
            _logger = Logger.Instance;
            _logger.OnLogFormattedMessage += Console.WriteLine;
            _notificationManager = notificationManager;
            _settings = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Запуск MPIC Worker");

            if (_settings.MonitoredMailboxes == null || _settings.MonitoredMailboxes.Count == 0)
            {
                _logger.LogCritical("Ошибка: настройки мониторинга не найдены или пусты в appsettings.json.");
                await SendNotificationAsync("Критическая ошибка MPIC",
                    "Ошибка: настройки мониторинга не найдены или пусты в appsettings.json.");
                return;
            }

            _logger.LogInformation("Инициализация MPIC Worker завершена. Запуск цикла проверки.");

            // Создаём EmailService один раз на весь жизненный цикл
            var emailService = new EmailService(_settings.NotificationSettings, _logger);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("--- Начало цикла проверки ---");

                foreach (var mailbox in _settings.MonitoredMailboxes)
                {
                    await CheckMailboxIntegration(mailbox, _settings, emailService, stoppingToken);
                }

                _logger.LogInformation("--- Конец цикла проверки ---");

                if (_settings.RunIntervalMinutes > 0 && !stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation($"Следующий запуск через {_settings.RunIntervalMinutes} мин.");
                    try
                    {
                        await Task.Delay(_settings.RunIntervalMinutes * 60 * 1000, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Получен сигнал остановки службы. Завершение.");
                        break;
                    }
                }
                else
                {
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

            var match = Regex.Match(rawValue, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
            if (match.Success)
                return match.Value.ToLowerInvariant();

            return rawValue.ToLowerInvariant();
        }

        private async Task CheckMailboxIntegration(MailboxSettings mailbox, RootSettings rootSettings,
            EmailService emailService, CancellationToken stoppingToken)
        {
            int maxTimeToCreateDealAfterLetter = rootSettings.MaxTimeToCreateDealAfterLetter;
            bool skipReplyLetters = rootSettings.SkipReplyLetters;
            string mailboxId = mailbox.Username;

            _logger.LogInformation($"--- Проверка интеграции для ящика {mailboxId} ---");

            // Получаем время последнего обработанного письма (UTC)
            var lastProcessedUtc = _notificationManager.GetLastProcessedTimeUtc(mailboxId);

            // Получаем новые письма (после lastProcessedUtc) или последние 10, если ящик ещё не проверялся
            var emails = await GetRecentLetters(mailbox.Username, mailbox.Password, lastProcessedUtc);
            if (emails == null || emails.Count == 0)
            {
                _logger.LogInformation($"Новых писем для {mailboxId} с {lastProcessedUtc:yyyy-MM-dd HH:mm:ss} UTC не найдено.");
                return;
            }

            var newestEmail = emails.First();
            var oldestEmailInBatch = emails.Last();
            _logger.LogInformation($"Получено {emails.Count} писем. Самое новое: от {newestEmail.Sender} " +
                $"в {newestEmail.ReceivedDate:yyyy-MM-dd HH:mm:ss zzz}. Самое старое: от {oldestEmailInBatch.Sender} " +
                $"в {oldestEmailInBatch.ReceivedDate:yyyy-MM-dd HH:mm:ss zzz}.");

            // Запрашиваем сделки с запасом, чтобы гарантированно получить все, созданные от этих писем
            var oldestEmailUtc = oldestEmailInBatch.ReceivedDate.ToUniversalTime().DateTime;
            var apiQueryTime = oldestEmailUtc.AddHours(-1);

            var lastDeals = await GetLastDeals(apiQueryTime, rootSettings);
            if (lastDeals == null)
            {
                string errorMsg = $"Не удалось получить сделки из Мегаплана для проверки ящика {mailboxId}.";
                _logger.LogError(errorMsg, logToConsole: true);
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
                    _logger.LogInformation($"Пропущено письмо-ответ/пересылка от {email.Sender} (тема: \"{email.Subject ?? "(без темы)"}\").");
                    _notificationManager.UpdateLastProcessedTimeUtc(mailboxId, emailReceivedUtc);
                    processedCount++;
                    continue;
                }

                // Если прошло меньше порога — не обрабатываем это и все более старые письма
                if (elapsedMinutes < maxTimeToCreateDealAfterLetter)
                {
                    _logger.LogInformation($"Письмо от {email.Sender} от {emailReceivedUtc:yyyy-MM-dd HH:mm:ss} UTC: " +
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
                        _logger.LogInformation(resultMessage);

                        if (_notificationManager.ShouldSendSuccessNotification(mailboxId))
                        {
                            _logger.LogInformation("Обнаружено восстановление работы интеграции. Отправка уведомления.");
                            await emailService.SendNotificationAsync($"Восстановление интеграции MPIC: {mailboxId}",
                                $"Интеграция восстановлена. " +
                                $"Последняя успешная сделка создана для письма от {email.Sender} в {dealTimeCreated:yyyy-MM-dd HH:mm:ss}.");
                            _notificationManager.RecordSuccess(mailboxId);
                        }
                    }
                    else
                    {
                        // СДЕЛКА НАЙДЕНА, НО С ЗАДЕРЖКОЙ — предупреждение
                        isWarning = true;
                        resultMessage = $"Интеграция с ящиком {mailboxId} работает, но на создание сделки ушло {diffMinutes:F2} мин" +
                            $" (больше порога в {maxTimeToCreateDealAfterLetter} мин)." +
                            $" Письмо от {email.Sender} от {emailReceivedUtc:yyyy-MM-dd HH:mm:ss} UTC.";
                        _logger.LogWarning(resultMessage);

                        if (_notificationManager.ShouldSendFailureNotification(mailboxId, email.MessageId, email.Sender, emailReceivedUtc))
                        {
                            await emailService.SendNotificationAsync($"Предупреждение интеграции MPIC: {mailboxId}", resultMessage);
                            _notificationManager.RecordFailure(mailboxId, email.MessageId, email.Sender, emailReceivedUtc);
                        }
                    }
                }

                if (!isDealCreated && !isWarning)
                {
                    // СДЕЛКА НЕ НАЙДЕНА — сбой интеграции
                    resultMessage = $"ИНТЕГРАЦИЯ НЕ РАБОТАЕТ: Для ящика {mailboxId} не найдено ни одной сделки, " +
                        $"созданной после письма от <b>{email.Sender}</b>, полученного в {emailReceivedUtc:yyyy-MM-dd HH:mm:ss} UTC.";
                    _logger.LogError(resultMessage, logToConsole: true);

                    if (_notificationManager.ShouldSendFailureNotification(mailboxId, email.MessageId, email.Sender, emailReceivedUtc))
                    {
                        await emailService.SendNotificationAsync($"Сбой интеграции MPIC: {mailboxId}", resultMessage);
                        _notificationManager.RecordFailure(mailboxId, email.MessageId, email.Sender, emailReceivedUtc);
                    }
                }

                // Помечаем письмо как обработанное
                _notificationManager.UpdateLastProcessedTimeUtc(mailboxId, emailReceivedUtc);
                processedCount++;
            }

            if (processedCount > 0)
            {
                _logger.LogInformation($"Писем обработано: {processedCount}");
            }
        }

        /// <summary>
        /// Получает последние письма из ящика, начиная с указанной даты (UTC).
        /// Если дата минимальная — берёт последние 10 писем.
        /// </summary>
        private async Task<List<EmailDetails>?> GetRecentLetters(string username, string password, DateTime lastProcessedUtc)
        {
            var emailReader = new EmailReader(username, password);
            var emailDetailsList = await emailReader.GetRecentEmailsAsync(lastProcessedUtc, count: 10);

            if (emailDetailsList == null || emailDetailsList.Count == 0)
            {
                _logger.LogInformation("Новых писем не найдено.");
                return null;
            }

            _logger.LogInformation($"Получено писем: {emailDetailsList.Count}. " +
                $"Самое новое: {emailDetailsList.First().ReceivedDate:yyyy-MM-dd HH:mm:ss zzz}");

            return emailDetailsList;
        }

        private async Task<List<Deal>> GetLastDeals(DateTime targetDateTimeUtc, RootSettings settings)
        {
            if (settings.LaunchTime == null
                || settings.LaunchTime.Length == 0
                || string.IsNullOrWhiteSpace(settings.Username)
                || string.IsNullOrWhiteSpace(settings.Password)
                || string.IsNullOrWhiteSpace(settings.BaseApUrl)
                || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                _logger.LogCritical("Ошибка: некорректные настройки для доступа к API Мегаплана.");
                return null;
            }

            IApiClient apiClient = new MegaApiClient(logger: _logger, tokenFile: Consts.TOKEN_FILE_MEGAPLAN,
                tokenExpAtFile: Consts.TOKEN_EXP_AT_FILE_MEGAPLAN, baseApiUrl: settings.BaseApUrl,
                username: settings.Username, password: settings.Password);
            IApiDataMapper apiDataMapper = new ApiDataMapper(_logger);
            ApiService apiService = new(_logger, apiClient, apiDataMapper);

            List<Deal> deals = await apiService.GetAndMapDealsUpdatedAfter(targetDateTimeUtc);
            return deals;
        }

        /// <summary>
        /// Отправляет уведомление через EmailService, создавая его временно на основе настроек.
        /// Используется для критических ошибок до входа в основной цикл.
        /// </summary>
        private async Task SendNotificationAsync(string subject, string body)
        {
            var emailService = new EmailService(_settings.NotificationSettings, _logger);
            await emailService.SendNotificationAsync(subject, body);
        }

        public override void Dispose()
        {
            _logger.LogInformation("MPIC Worker остановлен.");
            base.Dispose();
        }
    }
}