using MegaplanSync.Core;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models.Contractor;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Models.DealStatusHistory;
using MegaplanSync.Logging;
using System.Text.Json;

namespace MegaplanSync.Service;

public class ApiService(ILogger logger, IApiClient apiClient, IApiDataMapper dataMapper)
{
    private readonly IApiClient _apiClient = apiClient;
    private readonly IApiDataMapper _dataMapper = dataMapper;

    public async Task<Deal> GetAndMapDeal(long id)
    {
        logger.LogDebug($"Получаю сделку {id} из Мегаплана");
        string jsonPayload = DealPayload.GetPayload();
        var json = await _apiClient.GetEntityJsonAsync(Consts.ENTITY_NAME_DEAL, id, null, jsonPayload);
        if (json != null)
        {
            var deal = _dataMapper.MapEntity<Deal>(json);
            if (deal != null)
            {
                var normalizer = new DataNormalizer(Consts.MAPPING_DEAL_RULES_FILE);
                normalizer.Normalize(deal);
                return deal;
            }
        }
        logger.LogDebug($"Сделка с id '{id}' в Мегаплане отсутствует");
        return null;
    }

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

            result.AddRange(deals.Where(d => d.TimeUpdated.Value > afterDateTime));

            if (oldestDeal.TimeUpdated.Value <= afterDateTime)
            {
                break;
            }

            lastId = long.Parse(oldestDeal.Id);
            logger.LogInformation($"Ищем после Id: {lastId}");
        }

        logger.LogDebug($"Всего получено сделок: {result.Count}");
        return result;
    }

    public async Task<List<Deal>> GetAndMapActiveDeals(long id = Consts.ID_BEFORE_START_ID, int limit = Consts.JSON_ENTRIES_LIMIT)
    {
        logger.LogDebug($"Получаю активные сделки из Мегаплана, созданные после id: {id}");
        var totalDeals = new List<Deal>();
        var deals = new List<Deal>();

        do
        {
            string jsonPayload = DealPayload.GetActiveDealsPayload(afterId: id, limit: limit);
            var json = await _apiClient.GetEntitiesJsonAsync(Consts.ENTITY_NAME_DEAL, jsonPayload);
            if (json != null)
            {
                deals = _dataMapper.MapEntities<Deal>(json);
                var normalizer = new DataNormalizer(Consts.MAPPING_DEAL_RULES_FILE);
                foreach (var deal in deals)
                {
                    normalizer.Normalize(deal);
                }
                if (deals.Count > 0)
                {
                    totalDeals.AddRange(deals);
                    id = Logic.GetNewestId(deals);
                }
            }
            else
            {
                break;
            }
        }
        while (deals.Count > 0);
        return totalDeals;
    }

    public async Task<List<Deal>> GetAndMapNotNegativeDeals(long id = Consts.ID_BEFORE_START_ID, int limit = Consts.JSON_ENTRIES_LIMIT)
    {
        logger.LogDebug($"Получаю активные сделки из Мегаплана, созданные после id: {id}");
        var totalDeals = new List<Deal>();
        var deals = new List<Deal>();

        do
        {
            string jsonPayload = DealPayload.GetNotNegativeDealsPayload(afterId: id, limit: limit);
            var json = await _apiClient.GetEntitiesJsonAsync(Consts.ENTITY_NAME_DEAL, jsonPayload);
            if (json != null)
            {
                deals = _dataMapper.MapEntities<Deal>(json);
                var normalizer = new DataNormalizer(Consts.MAPPING_DEAL_RULES_FILE);
                foreach (var deal in deals)
                {
                    normalizer.Normalize(deal);
                }
                if (deals.Count > 0)
                {
                    totalDeals.AddRange(deals);
                    id = Logic.GetNewestId(deals);
                }
            }
            else
            {
                break;
            }
        }
        while (deals.Count > 0);
        return totalDeals;
    }

    /// <summary>
    /// заправшивает записи после указанного id (должен существовать), делает json запросы пока возвращаются данные
    /// в нагрузке обязательно должен быть элемент pageAfter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entityName"></param>
    /// <param name="jsonPayload"></param>
    /// <param name="normalizer"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<List<T>> GetAndMapEntitiesAfterId<T>(string entityName, string jsonPayload, long id, DataNormalizer normalizer = null) where T : class, IHasId
    {
        if (!jsonPayload.Contains("pageAfter")) throw new Exception("Передана неверная нагрузка");

        logger.LogDebug($"Получаю записи {typeof(T).Name} из Мегаплана  (несколько страниц)");
        var total = new List<T>();
        var part = new List<T>();

        do
        {
            var prevId = id;
            var json = await _apiClient.GetEntitiesJsonAsync(entityName, jsonPayload);
            if (json != null)
            {
                part = _dataMapper.MapEntities<T>(json);
                if (normalizer != null)
                {
                    foreach (var entity in part)
                    {
                        normalizer.Normalize(entity);
                    }
                }
                if (part.Count > 0)
                {
                    total.AddRange(part);
                    id = Logic.GetNewestId(part);
                    jsonPayload = jsonPayload.Replace(prevId.ToString(), id.ToString());
                }
            }
            else
            {
                break;
            }
        }
        while (part.Count > 0);
        return total;
    }

    public async Task<List<T>> GetAndMapAllEntities<T>(string entityName, string jsonPayload, 
        DataNormalizer normalizer = null, string filterId = null) where T : class, IHasId, IHasPayload
    {
        logger.LogDebug($"Получаю записи {typeof(T).Name} из Мегаплана  (несколько страниц)");
        var total = new List<T>();
        var part = new List<T>();
        var id = Consts.ID_BEFORE_START_ID;
        do
        {
            var prevId = id;
            var json = await _apiClient.GetEntitiesJsonAsync(entityName, jsonPayload);
            if (json != null)
            {
                part = _dataMapper.MapEntities<T>(json);
                if (normalizer != null)
                {
                    foreach (var entity in part)
                    {
                        normalizer.Normalize(entity);
                    }
                }
                if (part.Count > 0)
                {
                    total.AddRange(part);
                    id = Logic.GetNewestId(part);
                    if(!jsonPayload.Contains("pageAfter"))
                    {
                        jsonPayload = T.GetPayload(afterId: id, filterId: filterId);
                        continue;
                    }
                    jsonPayload = jsonPayload.Replace(prevId.ToString(), id.ToString());
                }
            }
            else
            {
                break;
            }
        }
        while (part.Count > 0);
        return total;
    }

    // используется только в тестах
    /// <summary>
    /// делает один json-запрос (данные в пределах лимита jsonPayload)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entetyName"></param>
    /// <param name="jsonPayload"></param>
    /// <param name="normalizer"></param>
    /// <returns></returns>
    public async Task<List<T>> GetAndMapEntities<T>(string entetyName, string jsonPayload, DataNormalizer normalizer = null) where T : class
    {
        logger.LogDebug($"Получаю записи {typeof(T).Name} из Мегаплана (одна страница)");
        var json = await _apiClient.GetEntitiesJsonAsync(entetyName, jsonPayload);
        if (json != null)
        {
            var entities = _dataMapper.MapEntities<T>(json);
            if (entities.Count != 0)
            {
                if (normalizer != null)
                {
                    foreach (var entety in entities)
                    {
                        normalizer.Normalize(entety);
                    }
                }
                return entities;
            }
        }
        logger.LogDebug($"Вернулась пустая коллекция");
        return [];
    }

    public async Task<List<(DealStatusHistory, long)>> GetAndMapDealStatusHystoryWithDealId(long dealId)
    {
        logger.LogDebug($"Получаю историю изменения статуса сделки с id: {dealId}");
        var json = await _apiClient.GetEntityJsonAsync(Consts.ENTITY_NAME_DEAL, dealId,
            Consts.ENTITY_NAME_DEAL_HISTORY_STATUS);
        if (json != null)
        {
            var dealStatusHistory = _dataMapper.MapEntities<DealStatusHistory>(json);

            if (dealStatusHistory != null)
            {
                var dealStatusHistoryWithDealId = dealStatusHistory.Select(sh => (sh, dealId)).ToList();
                return dealStatusHistoryWithDealId;
            }
        }
        logger.LogDebug($"История изменения статуса сделки с id '{dealId}' в Мегаплане отсутствует");
        return null;
    }

    /// <summary>
    /// возвращает все записи в пределах лимита api
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entityName"></param>
    /// <returns></returns>
    public async Task<List<T>> GetAndMapEntitiesWithoutPayload<T>(string entityName)
    {
        logger.LogDebug($"Получаю записи из Мегаплана");
        var json = await _apiClient.GetEntitiesJsonAsync(entityName);
        var entities = _dataMapper.MapEntities<T>(json);
        return entities;
    }

    // используется только в тестах
    public async Task<Contractor> GetAndMapContractor(long id)
    {
        logger.LogDebug($"Получаю клиента {id} из Мегаплана");
        string jsonPayload = ContractorPayload.GetPayload();
        var json = await _apiClient.GetEntityJsonAsync(Consts.ENTITY_NAME_CONTRACTOR, id, null, jsonPayload);
        if (json != null)
        {
            var contractor = _dataMapper.MapEntity<Contractor>(json);
            if (contractor != null)
            {
                return contractor;
            }
        }
        logger.LogDebug($"Клиент с id '{id}' в Мегаплане отсутствует");
        return null;
    }

    public async Task<string> UpdateDealInCrm(long id, Dictionary<string, object> fieldsToUpdate)
    {
        if (fieldsToUpdate == null || fieldsToUpdate.Count == 0)
        {
            logger.LogWarning($"Попытка обновить сделку {id} без полей.");
            return null;
        }

        string jsonPayload = JsonSerializer.Serialize(fieldsToUpdate);

        logger.LogDebug($"Отправляю POST-запрос для сделки {id} с данными: {jsonPayload}");

        var jsonResponse = await _apiClient.PostEntityJsonAsync(Consts.ENTITY_NAME_DEAL, id, jsonPayload);

        return jsonResponse;
    }

    
}
