using MySql.Data.MySqlClient;
using MegaplanSync.Core.Interfaces;
using System.Data;

namespace MegaplanSync.DataAccess.MySql;

public class MySqlDataAccess : IDataAccess, IDisposable
{
    private readonly ILogger _logger;
    private readonly string _connectionString;

    public MySqlDataAccess(ILogger logger, string connectionString)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _logger.LogError("Строка подключения к базе данных MySql не задана при инициализации.");
            throw new ArgumentException("Строка подключения не может быть пустой.", nameof(connectionString));
        }
    }

    /// <summary>
    /// Выполняет SQL-команду, не возвращающую набор данных (INSERT, UPDATE, DELETE).
    /// </summary>
    /// <param name="sqlQuery">SQL-запрос для выполнения.</param>
    /// <param name="parameters">Необязательный словарь параметров для запроса (для предотвращения SQL-инъекций).</param>
    /// <returns>Количество затронутых строк.</returns>
    /// <exception cref="DataAccessLayerException">Выбрасывается при ошибках работы с БД.</exception>
    public async Task<int> ExecuteAsync(string sqlQuery, Dictionary<string, object>? parameters = null)
    {
        using MySqlConnection connection = new MySqlConnection(_connectionString);
        using MySqlCommand command = new MySqlCommand(sqlQuery, connection);

        AddParametersToCommand(command, parameters);

        try
        {
            await connection.OpenAsync();
            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected;
        }
        catch (MySqlException mySqlEx)
        {
            _logger.LogError($"Ошибка MySql при выполнении команды: {mySqlEx.Message}. Запрос: {sqlQuery}", mySqlEx);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Неизвестная ошибка при выполнении SQL-команды: {ex.Message}. Запрос: {sqlQuery}", ex);
            throw;
        }
    }

    public async Task<object?> ExecuteScalarAsync(string sqlQuery)
    {
        using MySqlConnection connection = new MySqlConnection(_connectionString);
        using MySqlCommand command = new MySqlCommand(sqlQuery, connection);

        try
        {
            await connection.OpenAsync();
            object? result = await command.ExecuteScalarAsync();
            return result;
        }
        catch (MySqlException mySqlEx)
        {
            _logger.LogError($"Ошибка MySql при выполнении скалярной команды: {mySqlEx.Message}. Запрос: {sqlQuery}", mySqlEx);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Неизвестная ошибка при выполнении скалярной SQL-команды: {ex.Message}. Запрос: {sqlQuery}", ex);
            throw;
        }
    }

    /// <summary>
    /// Выполняет SQL-запрос SELECT и возвращает результат в виде DataTable (асинхронно).
    /// </summary>
    /// <param name="sqlQuery">Строка SQL-запроса SELECT.</param>
    /// <param name="parameters">Необязательный словарь параметров для запроса.</param>
    /// <returns>Объект DataTable с результатами запроса.</returns>
    /// <exception cref="DataAccessLayerException">Выбрасывается при ошибках работы с БД.</exception>
    public async Task<DataTable> SelectAsync(string sqlQuery, Dictionary<string, object>? parameters = null)
    {
        using MySqlConnection connection = new MySqlConnection(_connectionString);
        using MySqlCommand command = new MySqlCommand(sqlQuery, connection);

        AddParametersToCommand(command, parameters);

        DataTable dataTable = new DataTable();
        try
        {
            await connection.OpenAsync();
            using (var adapter = new MySqlDataAdapter(command))
            {
                await Task.Run(() => adapter.Fill(dataTable)); 
            }
            return dataTable;
        }
        catch (MySqlException mySqlEx)
        {
            _logger.LogError($"Ошибка MySql при выполнении SELECT-запроса: {mySqlEx.Message}. Запрос: {sqlQuery}", mySqlEx);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Неизвестная ошибка при выполнении SELECT-запроса: {ex.Message}. Запрос: {sqlQuery}", ex);
            throw;
        }
    }

    /// <summary>
    /// Вынесенный вспомогательный метод для добавления параметров.
    /// </summary>
    private void AddParametersToCommand(MySqlCommand command, Dictionary<string, object>? parameters)
    {
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                if (!param.Key.StartsWith("@"))
                {
                    command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }
        }
    }

    /// <summary>
    /// Реализация интерфейса IDisposable. В этом варианте нет долгоживущих ресурсов,
    /// но можно оставить для единообразия, если IDataAccess требует IDisposable.
    /// </summary>
    public void Dispose()
    {
        
    }

    public class DataAccessLayerException : Exception
    {
        public DataAccessLayerException() { }
        public DataAccessLayerException(string message) : base(message) { }
        public DataAccessLayerException(string message, Exception innerException) : base(message, innerException) { }
    }
}