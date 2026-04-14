using MegaplanSync.ApiClient;
using MegaplanSync.Core;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Logging;
using MegaplanSync.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            var monitoringSettings = serializer.LoadEntityFromFile<MonitoringSettings>(Consts.APP_SETTINGS_FILE);

            if (monitoringSettings == null || monitoringSettings.MonitoredMailboxes == null || monitoringSettings.MonitoredMailboxes.Count == 0)
            {
                logger.LogCritical("Ошибка: настройки мониторинга не найдены или пусты в appsettings.json.");
                Console.ReadLine();
                return;
            }

            foreach (var mailbox in monitoringSettings.MonitoredMailboxes)
            {
                await CheckMailboxIntegration(logger, mailbox, monitoringSettings.MaxTimeToCreateDealAfterLetter);
            }

            Console.ReadLine();
        }

        private static async Task CheckMailboxIntegration(ILogger logger, MailboxSettings mailbox, int maxTimeToCreateDealAfterLetter)
        {
            logger.LogInformation($"--- Проверка интеграции для ящика {mailbox.Username} ---");

            var lastLetter = await GetLastLetterInfo(logger, mailbox.Username, mailbox.Password);
            if (lastLetter == null)
            {
                logger.LogError($"Не удалось получить последнее письмо для {mailbox.Username}.");
                return;
            }

            var targetDateTime = lastLetter.ReceivedDate.ToLocalTime().DateTime;
            var lastDeals = await GetLastDeals(logger, targetDateTime);
            if (lastDeals == null)
            {
                 logger.LogError($"Не удалось получить сделки для проверки ящика {mailbox.Username}.");
                 return;
            }

            var match = lastDeals.Where(d => d.Contractor?.FirstName == lastLetter.Sender).ToList();
            
            if (match.Any())
            {
                var dealTimeCreated = match.First().TimeCreated.Value;
                var diff = (dealTimeCreated - targetDateTime).TotalMinutes;
                
                if (diff < maxTimeToCreateDealAfterLetter)
                {
                    logger.LogInformation($"Интеграция с ящиком {mailbox.Username} работает исправно. Письмо получено в {targetDateTime}. Сделка создана в {dealTimeCreated}, через {diff:F2} мин.");
                }
                else
                {
                    logger.LogWarning($"Интеграция с ящиком {mailbox.Username} работает, но на создание сделки ушло {diff:F2} минут (больше порога в {maxTimeToCreateDealAfterLetter} мин).");
                }
            }
            else
            {
                logger.LogError($"ИНТЕГРАЦИЯ НЕ РАБОТАЕТ: Для ящика {mailbox.Username} не найдено ни одной сделки, созданной после письма от {lastLetter.Sender} ({targetDateTime}).");
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
                logger.LogError("Не удалось получить данные о последнем письме");
                return null;
            }
        }
    }
}
