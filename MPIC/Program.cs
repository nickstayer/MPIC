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
            var username1 = "autodeal@chelzeo.ru";
            var password1 = "xkvrztrnmlxfdmgj";

            var username2 = "chelzeomarket@chelzeo.ru";
            var password2 = "syzwzbehbqugbsnk";

            var maxTimeToCreateDealAfterLetter = 5;

            Logger.Initialize();
            ILogger logger = Logger.Instance;
            logger.OnLogFormattedMessage += Console.WriteLine;
            logger.LogInformation("Инициализация");


            var lastLetter1 = await GetLastLetterInfo(logger, username1, password1);
            var targetDateTime1 = lastLetter1.ReceivedDate.ToLocalTime().DateTime;
            var lastDeals1 = await GetLastDeals(logger, targetDateTime1);
            var match1 = lastDeals1.Where(d => d.Contractor?.FirstName == lastLetter1.Sender).ToList();
            var dealTimeCreated1 = match1?.FirstOrDefault()?.TimeCreated.Value;
            var diff1 = (dealTimeCreated1 - targetDateTime1)?.TotalMinutes;
            var diffLessThanSpecifiedPeriod1 = diff1 < maxTimeToCreateDealAfterLetter;

            if (match1.Count > 0 && diffLessThanSpecifiedPeriod1)
            {
                logger.LogInformation($"Интеграция с ящиком {username1} работает исправно");
            }
            else if (match1.Count > 0 && !diffLessThanSpecifiedPeriod1)
            {
                logger.LogInformation($"Интеграция с ящиком {username1} работает исправно, но на создание сделки ушло {diff1} минут");
            }
            else if (match1.Count == 0)
            {
                logger.LogInformation($"Интеграция с ящиком {username1} НЕ работает");
            }


            var lastLetter2 = await GetLastLetterInfo(logger, username2, password2);
            var targetDateTime2 = lastLetter2.ReceivedDate.ToLocalTime().DateTime;
            var lastDeals2 = await GetLastDeals(logger, targetDateTime2);
            var match2 = lastDeals2.Where(d => d.Contractor?.FirstName == lastLetter2.Sender).ToList();
            var dealTimeCreated2 = match2?.FirstOrDefault()?.TimeCreated.Value;
            var diff2 = (dealTimeCreated2 - targetDateTime2)?.TotalMinutes;
            var diffLessThanSpecifiedPeriod2 = diff2 < maxTimeToCreateDealAfterLetter;

            if (match2.Count > 0 && diffLessThanSpecifiedPeriod2)
            {
                logger.LogInformation($"Интеграция с ящиком {username2} работает исправно");
            }
            else if (match2.Count > 0 && !diffLessThanSpecifiedPeriod2)
            {
                logger.LogInformation($"Интеграция с ящиком {username2} работает исправно, но на создание сделки ушло {diff2} минут");
            }
            else if (match2.Count == 0)
            {
                logger.LogInformation($"Интеграция с ящиком {username2} НЕ работает");
            }
            Console.ReadLine();
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
                logger.LogCritical("Ошибка: некорректные настройки.");
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
                logger.LogInformation("Данные получены");
                return emailDetails;
            }
            else
            {
                logger.LogError("Не удалось получить данные");
                return null;
            }
        }
    }
}
