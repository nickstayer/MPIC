using MegaplanSync.Core;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Models.DealStatusHistory;
using MegaplanSync.DataAccess.MySql;

namespace MegaplanSync.Service;

public class DbService(ILogger logger, IDataAccess dataAccess,IDbDataMapper dataMapper)
{
    private readonly IDbDataMapper _dataMapper = dataMapper;
    private readonly IDataAccess _dataAccess = dataAccess;

    public async Task<int?> UpdateLog(DateTime dateTime = default)
    {
        logger.LogDebug($"Обновляю лог.");
        string query = QueryHelper.GetUpdateLogQuery(dateTime);
        try
        {
            int rowsAffected = await _dataAccess.ExecuteAsync(query);
            logger.LogDebug($"Успешно");
            return rowsAffected;
        }
        catch (MySqlDataAccess.DataAccessLayerException ex)
        {
            logger.LogError($"Ошибка базы данных при обновлении лога: {ex.Message}", ex);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError($"Неизвестная ошибка при обновлении лога: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<DateTime> GetLastUpdateDateFromDbLog()
    {
        logger.LogDebug("Получаю дату последнего обновления из лога базы данных");
        var query = QueryHelper.GetLastUpdateDateFromDbLogQuery();
        var dataTable = await _dataAccess.SelectAsync(query);
        if (dataTable == null || dataTable.Rows.Count == 0)
        {
            return default;
        }
        var row = dataTable.Rows[0];
        var val = row["updated"]?.ToString();
        var result = Logic.ParseLogDateTime(val);
        return result;
    }

    public async Task<Deal?> GetAndMapLastDeal()
    {
        logger.LogDebug($"Получаю последнюю сделку из базы данных");
        var query = QueryHelper.GetLastEntityQuery(Consts.TABLE_NAME_DEAL);
        var dataTable = await _dataAccess.SelectAsync(query);
        var deal = _dataMapper.MapDeals(dataTable).FirstOrDefault();

        return deal;
    }

    public async Task<List<T>> GetAndMapEnteties<T>(string tableName, DataNormalizer normalizer = null) where T : class, new()
    {
        logger.LogDebug($"Получаю все записи из базы данных");
        var query = QueryHelper.GetEntitiesQuery(tableName);
        var dataTable = await _dataAccess.SelectAsync(query);
        var dataMapper = new DbGenericDataMapper<T>(logger);
        var entities = dataMapper.Map(dataTable, normalizer);
        return entities;
    }

    public async Task<Deal?> GetAndMapDeal(long id)
    {
        logger.LogDebug($"Получаю сделку {id} из базы данных");
        var query = QueryHelper.GetEntityQuery(Consts.TABLE_NAME_DEAL, id);
        var dataTable = await _dataAccess.SelectAsync(query);
        var deal = _dataMapper.MapDeals(dataTable).FirstOrDefault();
        return deal;
    }

    public async Task<List<Deal>> GetAndMapActiveDeals()
    {
        logger.LogDebug($"Получаю активные сделки из базы данных");
        var query = QueryHelper.GetActiveDealsQuery();
        var dataTable = await _dataAccess.SelectAsync(query);
        var deals = _dataMapper.MapDeals(dataTable);
        return deals;
    }

    public async Task<int> UpdateEntity(long id, string tableName,Dictionary<string, object?> updatedFields)
    {
        logger.LogInformation($"Обновляю запись {id} в таблице {tableName}.");

        string query = QueryHelper.GetUpdateEntityQuery(id, tableName, updatedFields);
        var parameters = updatedFields;

        try
        {
            int rowsAffected = await _dataAccess.ExecuteAsync(query, parameters);
            logger.LogDebug("Успешно");
            return rowsAffected;
        }
        catch (MySqlDataAccess.DataAccessLayerException ex)
        {
            logger.LogError($"Ошибка базы данных при обновлении записи {id}: {ex.Message}", ex);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError($"Неизвестная ошибка при обновлении записи {id}: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<int> UpdateEntity(string uniqFieldName, string uniqFieldValue, string tableName, Dictionary<string, object?> updatedFields)
    {
        logger.LogInformation($"Обновляю запись {uniqFieldValue} в таблице {tableName}.");

        string query = QueryHelper.GetUpdateEntityQuery(uniqFieldName, uniqFieldValue, tableName, updatedFields);
        var parameters = updatedFields;

        try
        {
            int rowsAffected = await _dataAccess.ExecuteAsync(query, parameters);
            logger.LogDebug("Успешно");
            return rowsAffected;
        }
        catch (MySqlDataAccess.DataAccessLayerException ex)
        {
            logger.LogError($"Ошибка базы данных при обновлении записи {uniqFieldValue}: {ex.Message}", ex);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError($"Неизвестная ошибка при обновлении записи {uniqFieldValue}: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<int> SetEmployeeFired(string employeeName)
    {
        logger.LogInformation($"Обновляю запись {employeeName}. Сотрудник заблокирован и уволен");

        string query = QueryHelper.GetUpdateEmployeeFiredQuery(employeeName);

        try
        {
            int rowsAffected = await _dataAccess.ExecuteAsync(query);
            logger.LogDebug("Успешно");
            return rowsAffected;
        }
        catch (MySqlDataAccess.DataAccessLayerException ex)
        {
            logger.LogError($"Ошибка базы данных при обновлении записи {employeeName}: {ex.Message}", ex);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError($"Неизвестная ошибка при обновлении записи {employeeName}: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<int> DeleteEntity(long id, string tableName)
    {
        logger.LogDebug($"Удаляю запись {id} из таблицы {tableName}.");
        string query = QueryHelper.GetDeleteEntityQuery(tableName, id);
        try
        {
            int rowsAffected = await _dataAccess.ExecuteAsync(query);
            logger.LogDebug("Успешно");
            return rowsAffected;
        }
        catch (MySqlDataAccess.DataAccessLayerException ex)
        {
            logger.LogError($"Ошибка базы данных при удалении записи {id}: {ex.Message}", ex);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError($"Неизвестная ошибка при удалении записи {id}: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<long> GetLastId(string tableName)
    {
        logger.LogDebug($"Получаю последний id из таблицы {tableName} базы данных");
        var query = QueryHelper.GetLastIdQuery(tableName);
        var dataTable = await _dataAccess.SelectAsync(query);
        if (dataTable.Rows.Count == 0) return 0;
        var row = dataTable.Rows[0];
        var val = row["id"].ToString();
        long id = val == null ? 0 : long.Parse(val);
        return id;
    }

    public async Task<int> Insert<T>(T entity, string tableName) where T : class?, IHasId
    {
        logger.LogInformation($"Добавляю {typeof(T).Name} {entity?.Id} в базу данных.");

        if (entity == null)
        {
            logger.LogWarning("Попытка вставить null-объект");
            return 0;
        }

        string query = QueryHelper.GetInsertQuery<T>(tableName);
        var parameters = new ParametersHelper(logger).GetParameters(entity);

        try
        {
            int rowsAffected = await _dataAccess.ExecuteAsync(query, parameters);
            logger.LogDebug("Успешно");
            return rowsAffected;
        }
        catch (MySqlDataAccess.DataAccessLayerException ex)
        {
            logger.LogError($"Ошибка базы данных при добавлении {entity.ToString} {entity?.Id}: {ex.Message}", ex);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError($"Неизвестная ошибка при добавлении {entity.ToString} {entity?.Id}: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<bool> IsDealStatusHistoryExists((DealStatusHistory, long) statusHistoryAndDealId)
    {
        if (statusHistoryAndDealId.Item1?.Id == null)
        {
            logger.LogDebug("Идентификатор истории статуса сделки равен null.");
            return false;
        }

        logger.LogDebug($"Проверяю есть ли в БД запись с ID: {statusHistoryAndDealId.Item1.Id}");

        string query = QueryHelper.GetSelectCountQuery(
            Consts.TABLE_NAME_DEAL_HISTORY_STATUS,
            long.Parse(statusHistoryAndDealId.Item1.Id)
        );

        try
        {
            var returnedObj = await _dataAccess.ExecuteScalarAsync(query);

            int rowsFound = Convert.ToInt32(returnedObj);
            bool exists = rowsFound > 0;

            logger.LogDebug($"Запись с ID {statusHistoryAndDealId.Item1.Id} найдена: {exists}. Количество: {rowsFound}");

            return exists;
        }
        catch (Exception ex)
        {
            logger.LogError($"Ошибка БД при проверке существования записи с ID {statusHistoryAndDealId.Item1.Id}: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> IsEntryExists(string id, string tableName)
    {
        if (id == null)
        {
            logger.LogDebug("Идентификатор равен null.");
            return false;
        }

        logger.LogDebug($"Проверяю есть ли в БД запись с ID: {id}");

        string query = QueryHelper.GetSelectCountQuery(
            tableName,
            long.Parse(id)
        );

        try
        {
            var returnedObj = await _dataAccess.ExecuteScalarAsync(query);

            int rowsFound = Convert.ToInt32(returnedObj);
            bool exists = rowsFound > 0;

            logger.LogDebug($"Запись с ID {id} найдена: {exists}. Количество: {rowsFound}");

            return exists;
        }
        catch (Exception ex)
        {
            logger.LogError($"Ошибка БД при проверке существования записи с ID {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<int> InsertDealStatusHistory((DealStatusHistory, long) statusHistoryAndDealId)
    {
        logger.LogInformation($"Добавляю историю статуса {statusHistoryAndDealId.Item1?.Id} в базу данных.");

        if (statusHistoryAndDealId.Item1 == null)
        {
            logger.LogWarning("Попытка вставить null-объект Deal.");
            return 0;
        }

        string query = QueryHelper.GetInsertQuery<DealStatusHistory>(Consts.TABLE_NAME_DEAL_HISTORY_STATUS, "dealId");

        var parameters = new ParametersHelper(logger).GetParameters(
        statusHistoryAndDealId.Item1,
        new Dictionary<string, object> { { "dealId", statusHistoryAndDealId.Item2 } }
        );

        try
        {
            int rowsAffected = await _dataAccess.ExecuteAsync(query, parameters);
            logger.LogDebug("Успешно");
            return rowsAffected;
        }
        catch (MySqlDataAccess.DataAccessLayerException ex)
        {
            logger.LogError($"Ошибка базы данных при добавлении истории статуса {statusHistoryAndDealId.Item1.Id}: {ex.Message}", ex);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError($"Неизвестная ошибка при добавлении истории статуса {statusHistoryAndDealId.Item1.Id}: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<int> DeleteAllEntries(string tableName)
    {
        logger.LogDebug($"Удаляю все записи из таблицы {tableName}.");
        string query = QueryHelper.GetDeleteAllEntriesQuery(tableName);
        try
        {
            int rowsAffected = await _dataAccess.ExecuteAsync(query);
            logger.LogDebug("Успешно");
            return rowsAffected;
        }
        catch (MySqlDataAccess.DataAccessLayerException ex)
        {
            logger.LogError($"Ошибка базы данных при удалении всех записей: {ex.Message}", ex);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError($"Ошибка базы данных при удалении всех записей: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<bool> IsEmployeeWorking(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogDebug("Имя сотрудника пустое или null.");
            return false;
        }

        string query = QueryHelper.GetIsWorkingValueQuery(name);

        try
        {
            var returnedObj = await _dataAccess.ExecuteScalarAsync(query);
            if (returnedObj == null)
                return false;

            if (!int.TryParse(returnedObj.ToString(), out var intVal))
                return false;

            return intVal == 1;
        }
        catch (Exception ex)
        {
            logger.LogError($"Ошибка БД при проверке статуса сотрудника {name}: {ex}");
            throw;
        }
    }
}