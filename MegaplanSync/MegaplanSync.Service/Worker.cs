using MegaplanSync.Core;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models.Contractor;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Models.DealStatusHistory;
using MegaplanSync.Core.Models.Department;
using MegaplanSync.Core.Models.Employee;
using MegaplanSync.Core.Models.Todo;
using MegaplanSync.DataAccess.MySql;
using MegaplanSync.Logging;
using Mysqlx.Crud;

namespace MegaplanSync.Service;

public class Worker(ApiService apiService, DbService dbService, ILogger logger)
{
    string cacheFile = "dealsUpdatedAfterLastUpdateFromApi.txt";
    public async Task SyncDeals()
    {
        await UpdateDealsInDb();
        await AddNewDealsToDb();
    }


    public async Task<int> UpdateDealsInDb(DateTime lastUpdateDateTime = default)
    {
        logger.LogInformation($"Обновляю сделки");

        logger.LogInformation($"Метод: сверка не отрицательных в бд с апи");
        var activeDealsFromDb = await dbService.GetNotNegativeDealsQuery();
        if (lastUpdateDateTime == default)
        {
            lastUpdateDateTime = await dbService.GetLastUpdateDateFromDbLog();
            if (lastUpdateDateTime == default)
            {
                throw new Exception("Не удалось распарсить дату из лога");
            }
        }

        var dealsUpdatedAfterLastUpdateFromApi = await apiService.GetAndMapDealsUpdatedAfter(lastUpdateDateTime);

        var activeDealsFromDbDic = activeDealsFromDb
            .GroupBy(d => d.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var dealsUpdatedAfterLastUpdateFromApiDic = dealsUpdatedAfterLastUpdateFromApi
            .GroupBy(d => d.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var dups = dealsUpdatedAfterLastUpdateFromApi.Count - dealsUpdatedAfterLastUpdateFromApiDic.Count;
        logger.LogInformation($"Получено дублей из api: {dups}");

        if (File.Exists(cacheFile))
        {
            File.Delete(cacheFile);
        }

        if (dealsUpdatedAfterLastUpdateFromApiDic.Count > 0)
        {
            logger.LogInformation($"Кэширую id сделок в файл");
            File.WriteAllLines(cacheFile, dealsUpdatedAfterLastUpdateFromApiDic.Keys);
        }

        int counter = 0;
        foreach (var (dbId, dealFromDb) in activeDealsFromDbDic)
        {
            if (dealsUpdatedAfterLastUpdateFromApiDic.TryGetValue(dbId, out Deal dealFromApi))
            {
                if (Logic.IsTimeUpdatedDiff(dealFromDb, dealFromApi))
                {
                    var changedFields = Logic.GetDifferences(dealFromDb, dealFromApi);
                    if (changedFields.Count > 0)
                    {
                        counter += await dbService.UpdateEntity(long.Parse(dbId), Consts.TABLE_NAME_DEAL, changedFields);
                    }
                }
            }
        }

        var latestUpdateDeal = dealsUpdatedAfterLastUpdateFromApi.OrderByDescending(deal => deal.TimeUpdated.Value).FirstOrDefault();
        if (latestUpdateDeal != null)
        {
            logger.LogInformation($"Дата последней обновленной сделки: {latestUpdateDeal.TimeUpdated.Value}");
            await dbService.UpdateLog(latestUpdateDeal.TimeUpdated.Value);
        }
        else
        {
            logger.LogWarning("Не было получено обновленных сделок.");
        }

        var countOfUpdated = counter;

        logger.LogInformation($"Метод: сверка не отрицательных в апи с бд");
        var activeDealsFromApi = await apiService.GetAndMapNotNegativeDeals();

        var dbDealsIdSet = activeDealsFromDbDic.Keys.ToHashSet();
        var dealsToUpdateStatus = activeDealsFromApi
            .Where(apiDeal => !dbDealsIdSet.Contains(apiDeal.Id));
        foreach (var dealFromApi in dealsToUpdateStatus)
        {
            var dealFromDb = await dbService.GetAndMapDeal(long.Parse(dealFromApi.Id));
            if (dealFromDb == null)
            {
                logger.LogInformation($"Не нашел в БД сделку: {dealFromApi.Id}");
                continue;
            }

            var changedFields = Logic.GetDifferences(dealFromDb, dealFromApi);
            if (changedFields.Count > 0)
            {
                counter += await dbService.UpdateEntity(long.Parse(dealFromDb.Id), Consts.TABLE_NAME_DEAL, changedFields);
            }
        }
        var countOfActiveAgain = counter - countOfUpdated;

        logger.LogInformation($"Обновил записей: {counter}");
        logger.LogInformation($"Вновь активны: {countOfActiveAgain}");
        return counter;
    }


    public async Task<int> AddNewDealsToDb()
    {
        logger.LogInformation($"Добавляю новые сделки");
        var lastDealIdFromDb = await dbService.GetLastId(Consts.TABLE_NAME_DEAL);

        string jsonPayload = DealPayload.GetPayload(lastDealIdFromDb, Consts.JSON_ENTRIES_LIMIT);
        var normalizer = new DataNormalizer(Consts.MAPPING_DEAL_RULES_FILE);
        var newDealsFromMegaplan = await apiService.GetAndMapEntitiesAfterId<Deal>(Consts.ENTITY_NAME_DEAL, jsonPayload, lastDealIdFromDb, normalizer);

        int counter = 0;
        foreach (var deal in newDealsFromMegaplan)
        {
            counter += await dbService.Insert(deal, Consts.TABLE_NAME_DEAL);
        }
        logger.LogInformation($"Добавил записей: {counter}");
        return counter;
    }


    public async Task SyncDealsStatusHistory()
    {
        await AddNewDealsStatusesHistoryToDb();
    }


    public async Task<int> AddNewDealsStatusesHistoryToDb()
    {
        logger.LogInformation($"Добавляю историю статусов");
        if (!File.Exists(cacheFile))
        {
            logger.LogInformation($"Файл с кэшем отсутствует.");
            return 0;
        }
        logger.LogInformation($"Читаю кэш id сделок из файла");
        var lastUpdatedDealsId = File.ReadAllLines(cacheFile)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct();

        var statusesAndDealsIdTotal = new List<(DealStatusHistory, long)>();
        int counter = 0;
        foreach (var dealId in lastUpdatedDealsId)
        {
            var statusesAndDealsId = await apiService.GetAndMapDealStatusHystoryWithDealId(long.Parse(dealId));
            statusesAndDealsIdTotal.AddRange(statusesAndDealsId);
        }
        var statusesAndDealsIdTotalOrdered = statusesAndDealsIdTotal
            .OrderByDescending(x => long.Parse(x.Item1.Id));
        var existEntiesCounter = 0;
        foreach (var statusAndDealId in statusesAndDealsIdTotalOrdered)
        {
            if (!await dbService.IsDealStatusHistoryExists(statusAndDealId))
            {
                counter += await dbService.InsertDealStatusHistory(statusAndDealId);
            }
            else
            {
                // отсечка
                if (existEntiesCounter == 10)
                {
                    logger.LogInformation($"Отсечка: далее следуют уже добавленные статусы (в теории)");
                    break;
                }

                existEntiesCounter++;
            }
        }
        logger.LogInformation($"Добавил записей: {counter}");
        return counter;
    }


    internal async Task SyncContractors()
    {
        await AddNewContractorsToDb();
    }

    public async Task<int> AddNewContractorsToDb()
    {
        logger.LogInformation($"Добавляю новых клиентов");
        var lastEntityIdFromDb = await dbService.GetLastId(Consts.TABLE_NAME_CONTRACTOR);
        string jsonPayload = ContractorPayload.GetPayload(lastEntityIdFromDb);
        var newEntitiesFromMegaplan = await apiService.GetAndMapEntitiesAfterId<Contractor>(Consts.ENTITY_NAME_CONTRACTOR,
            jsonPayload, lastEntityIdFromDb);
        int counter = 0;
        foreach (var entity in newEntitiesFromMegaplan)
        {
            counter += await dbService.Insert(entity, Consts.TABLE_NAME_CONTRACTOR);
        }
        logger.LogInformation($"Добавил записей: {counter}");
        return counter;
    }

    internal async Task SyncEmployees()
    {
        await AddNewEmployeesToDb();
        await UpdateEmployeesInDb();
    }

    private async Task UpdateEmployeesInDb()
    {
        var normalizer = new DataNormalizer(Consts.MAPPING_EMPLOYEE_RULES_FILE, logger);
        var entitiesFromDb = await dbService.GetAndMapEnteties<Employee>(Consts.TABLE_NAME_EMPLOYEE, normalizer);
        var payload = EmployeePayload.GetPayload();
        var entitiesFromApi = await apiService.GetAndMapAllEntities<Employee>(Consts.ENTITY_NAME_EMPLOYEE,
            payload, normalizer);

        var entitiesFromDbDic = entitiesFromDb.ToDictionary(d => d.Name, d => d);
        var entitiesFromApiDic = entitiesFromApi.ToDictionary(d => d.Name, d => d);
        int updatedCount = 0;
        int firedCount = 0;
        foreach (var (name, entityFromDb) in entitiesFromDbDic)
        {
            if (entitiesFromApiDic.TryGetValue(name, out Employee entityFromApi))
            {
                var changedFields = Logic.GetDifferences(entityFromDb, entityFromApi);
                if (!entityFromApi.CanLogin)
                {
                    changedFields["isWorking"] = false;
                    changedFields["canLogin"] = false;
                }
                if (changedFields.Count > 0)
                {
                    updatedCount += await dbService.UpdateEntity("name", name, Consts.TABLE_NAME_EMPLOYEE, changedFields);
                }
            }
            else
            {
                if (await dbService.IsEmployeeWorking(name))
                {
                    firedCount += await dbService.SetEmployeeFired(name);
                }
            }
        }
        logger.LogInformation($"Обновил записей: {updatedCount + firedCount}");
        logger.LogInformation($"Уволено: {firedCount}");
    }

    public async Task<int> AddNewEmployeesToDb()
    {
        logger.LogInformation($"Добавляю новых сотрудников");
        var lastEntitiesIdFromDb = await dbService.GetLastId(Consts.TABLE_NAME_EMPLOYEE);
        string jsonPayload = EmployeePayload.GetPayload(lastEntitiesIdFromDb, Consts.JSON_ENTRIES_LIMIT);
        var newEntetiesFromMegaplan = await apiService.GetAndMapEntitiesAfterId<Employee>(Consts.ENTITY_NAME_EMPLOYEE, jsonPayload,
            lastEntitiesIdFromDb);
        int? counter = 0;
        foreach (var entety in newEntetiesFromMegaplan)
        {
            counter += await dbService.Insert(entety, Consts.TABLE_NAME_EMPLOYEE);
        }
        logger.LogInformation($"Добавил записей: {counter}");
        return counter.Value;
    }

    internal async Task SyncDepartments()
    {
        await AddNewDepartmentsToDb();
    }

    public async Task<int> AddNewDepartmentsToDb()
    {
        // сервер не воспринимал нагрузку afterId, поэтому подход изменен
        logger.LogInformation($"Добавляю новые подразделения");
        var allEntitiesFromApi = await apiService.GetAndMapEntitiesWithoutPayload<Department>(Consts.ENTITY_NAME_DEPARTMENT);
        var allEntitiesFromDb = await dbService.GetAndMapEnteties<Department>(Consts.TABLE_NAME_DEPARTMENT);
        var entetiesIdFromDb = allEntitiesFromDb.Select(x => x.Id).ToList();
        var newEntities = allEntitiesFromApi.Where(x => !entetiesIdFromDb.Contains(x.Id)).ToList();
        int counter = 0;
        foreach (var entity in newEntities)
        {
            counter += await dbService.Insert(entity, Consts.TABLE_NAME_DEPARTMENT);
        }
        logger.LogInformation($"Добавил записей: {counter}");
        return counter;
    }

    public async Task SyncTodos()
    {
        await AddNewTodosToDb();
    }

    public async Task<int> AddNewTodosToDb()
    {
        logger.LogInformation($"Добавляю новые задачи");
        var lastEntitiesIdFromDb = await dbService.GetLastId(Consts.TABLE_NAME_TODO);
        string jsonPayload = TodoPayload.GetPayload(lastEntitiesIdFromDb);
        var newEntetiesFromMegaplan = await apiService.GetAndMapEntitiesAfterId<Todo>(Consts.ENTITY_NAME_TODO,
            jsonPayload, lastEntitiesIdFromDb);
        int counter = 0;
        foreach (var entety in newEntetiesFromMegaplan)
        {
            counter += await dbService.Insert(entety, Consts.TABLE_NAME_TODO);
        }
        logger.LogInformation($"Добавил записей: {counter}");
        return counter;
    }

    public async Task SyncTodoStatuses()
    {
        await AddNewTodoStatusesToDb();

    }

    public async Task<int> AddNewTodoStatusesToDb()
    {
        logger.LogInformation($"Добавляю новые статусы задач");
        var allEntitiesFromApi = await apiService.GetAndMapEntitiesWithoutPayload<TodoStatus>(Consts.ENTITY_NAME_TODO_STATUS);
        var allEntitiesFromDb = await dbService.GetAndMapEnteties<TodoStatus>(Consts.TABLE_NAME_TODO_STATUS);
        var entetiesIpFromDb = allEntitiesFromDb.Select(x => x.Id).ToList();
        var newEntities = allEntitiesFromApi.Where(x => !entetiesIpFromDb.Contains(x.Id)).ToList();
        int counter = 0;
        foreach (var entity in newEntities)
        {
            counter += await dbService.Insert(entity, Consts.TABLE_NAME_TODO_STATUS);
        }
        logger.LogInformation($"Добавил записей: {counter}");
        return counter;
    }

    public async Task SyncTodoCategories()
    {
        await AddNewTodoCategoriesToDb();
    }

    public async Task<int> AddNewTodoCategoriesToDb()
    {
        logger.LogInformation($"Добавляю новые категории задач");
        var allEntitiesFromApi = await apiService.GetAndMapEntitiesWithoutPayload<TodoCategory>(Consts.ENTITY_NAME_TODO_CATEGORY);
        var allEntitiesFromDb = await dbService.GetAndMapEnteties<TodoCategory>(Consts.TABLE_NAME_TODO_CATEGORY);
        var entetiesIpFromDb = allEntitiesFromDb.Select(x => x.Id).ToList();
        var newEntities = allEntitiesFromApi.Where(x => !entetiesIpFromDb.Contains(x.Id)).ToList();
        int counter = 0;
        foreach (var entity in newEntities)
        {
            counter += await dbService.Insert(entity, Consts.TABLE_NAME_TODO_CATEGORY);
        }
        logger.LogInformation($"Добавил записей: {counter}");
        return counter;
    }

    public async Task BulkUpdateDealsInCrm(IEnumerable<long> dealIdsToUpdate, Dictionary<string, object> fieldsToUpdate)
    {
        var dealsToUpdate = new List<(long id, Dictionary<string, object> fields)>();

        foreach (var id in dealIdsToUpdate)
        {
            dealsToUpdate.Add((id, fieldsToUpdate));
        }

        logger.LogInformation($"Начинаю конкурентное обновление {dealsToUpdate.Count} сделок.");

        var successfulIds = await ProcessBulkUpdateDealsInCrmAsync(dealsToUpdate, Consts.MAX_CONCURRENCY);

        logger.LogInformation($"Успешно обновлено сделок: {successfulIds.Count}");
    }

    /// <summary>
    /// Массовое обновление сделок с ограничением параллелизма.
    /// </summary>
    private async Task<List<long>> ProcessBulkUpdateDealsInCrmAsync(
        IEnumerable<(long id, Dictionary<string, object> fields)> dealsToUpdate,
        int maxConcurrency)
    {
        var throttler = new SemaphoreSlim(maxConcurrency);
        var successIds = new List<long>();
        var updateTasks = new List<Task>();

        foreach (var deal in dealsToUpdate)
        {
            await throttler.WaitAsync();

            updateTasks.Add(Task.Run(async () =>
            {
                try
                {
                    var responseJson = await apiService.UpdateDealInCrm(deal.id, deal.fields);

                    if (responseJson != null)
                    {
                        lock (successIds)
                        {
                            successIds.Add(deal.id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Критическая ошибка при обновлении сделки {deal.id}: {ex}");
                }
                finally
                {
                    await Task.Delay(Consts.DELAY_PER_REQUEST_MS);
                    throttler.Release();
                }
            }));
        }

        await Task.WhenAll(updateTasks);
        return successIds;
    }
}