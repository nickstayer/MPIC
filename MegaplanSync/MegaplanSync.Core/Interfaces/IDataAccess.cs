using System.Data;

namespace MegaplanSync.Core.Interfaces;

public interface IDataAccess : IDisposable
{
    /// <summary>
    /// Выполняет SQL-команду, не возвращающую набор данных (INSERT, UPDATE, DELETE) асинхронно.
    /// </summary>
    /// <param name="sqlQuery">SQL-запрос для выполнения.</param>
    /// <param name="parameters">Необязательный словарь параметров для запроса (для предотвращения SQL-инъекций).</param>
    /// <returns>Количество затронутых строк.</returns>
    /// <exception cref="MegaplanSync.DataAccess.MySql.MySqlDataAccess.DataAccessLayerException">
    /// Выбрасывается при ошибках работы с БД.
    /// </exception>
    Task<int> ExecuteAsync(string sqlQuery, Dictionary<string, object>? parameters = null);
    Task<object?> ExecuteScalarAsync(string query);

    /// <summary>
    /// Выполняет SQL-запрос SELECT и возвращает результат в виде DataTable асинхронно.
    /// </summary>
    /// <param name="sqlQuery">Строка SQL-запроса SELECT.</param>
    /// <param name="parameters">Необязательный словарь параметров для запроса.</param>
    /// <returns>Объект DataTable с результатами запроса.</returns>
    /// <exception cref="MegaplanSync.DataAccess.MySql.MySqlDataAccess.DataAccessLayerException">
    /// Выбрасывается при ошибках работы с БД.
    /// </exception>
    Task<DataTable> SelectAsync(string sqlQuery, Dictionary<string, object>? parameters = null);
}
