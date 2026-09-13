using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models.Deal;

namespace MegaplanSync.Core;

/// <summary>
/// Сервис работы с API Мегаплана. В решении MPIC используется только
/// для получения сделок, обновлённых после заданного времени.
/// </summary>
public class ApiService(ILogger logger, IApiClient apiClient, IApiDataMapper dataMapper)
{
    private readonly IApiClient _apiClient = apiClient;
    private readonly IApiDataMapper _dataMapper = dataMapper;

    public async Task<List<Deal>> GetAndMapDealsUpdatedAfter(DateTime afterDateTime)
    {
        logger.LogDebug($"Получаю из Мегаплана сделки, обновленные после: {afterDateTime}");
        var result = new List<Deal>();
        long lastId = Consts.ID_BEFORE_START_ID;
        var normalizer = new DataNormalizer(Consts.MAPPING_DEAL_RULES_FILE);

        while (true)
        {
            string jsonPayload = DealPayload.GetLastUpdatedDealsPayload(afterId: lastId);
            var json = await _apiClient.GetEntitiesJsonAsync(Consts.ENTITY_NAME_DEAL, jsonPayload);
            if (string.IsNullOrEmpty(json))
                break;

            var deals = _dataMapper.MapEntities<Deal>(json);
            if (deals == null || deals.Count == 0)
                break;

            foreach (var deal in deals)
            {
                normalizer.Normalize(deal);
            }

            var oldestDeal = deals.Last();

            result.AddRange(deals.Where(d => d.TimeUpdated != null && d.TimeUpdated.Value > afterDateTime));

            if (oldestDeal.TimeUpdated == null || oldestDeal.TimeUpdated.Value <= afterDateTime)
            {
                break;
            }

            lastId = long.Parse(oldestDeal.Id);
            logger.LogInformation($"Ищем после Id: {lastId}");
        }

        logger.LogDebug($"Всего получено сделок: {result.Count}");
        return result;
    }
}
