using MegaplanSync.Core;
using MegaplanSync.DataAccess.MySql;
using System.Reflection;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace MegaplanSync.DataAccess.MySql;

public class QueryHelper
{
    public static string GetLastEntityQuery(string tableName)
    {
        string query = $"SELECT * FROM {tableName} ORDER BY id DESC LIMIT 1";
        return query;
    }

    public static string GetEntityQuery(string tableName, long id)
    {
        string query = $"SELECT * FROM {tableName} WHERE id={id}";
        return query;
    }

    public static string GetEntitiesQuery(string tableName)
    {
        string query = $"SELECT * FROM {tableName}";
        return query;
    }

    public static string GetDeleteEntityQuery(string tableName, long id)
    {
        string query = $"DELETE FROM {tableName} WHERE id='{id}';";
        return query;
    }


    public static string GetDeleteAllEntriesQuery(string tableName)
    {
        string query = $"DELETE FROM {tableName};";
        return query;
    }


    public static string GetSelectCountQuery(string tableName, long id)
    {
        string query = $"SELECT COUNT(*) FROM {tableName} WHERE id={id};";
        return query;
    }

    public static string GetInsertQuery<T>(string table, params string[] additionalColumns) where T : class
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var columnNames = properties
            .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
            .Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name)
            .ToList();

        columnNames.AddRange(additionalColumns);

        var escapedColumnNames = columnNames.Select(c => $"`{c}`").ToList();

        var parameterNames = columnNames.Select(c => $"@{c}").ToList();

        string columns = string.Join(", ", escapedColumnNames);
        string parameters = string.Join(", ", parameterNames);

        string query = $"INSERT INTO `{table}` ({columns}) VALUES ({parameters});";

        return query;
    }

    public static string GetUpdateLogQuery(DateTime dateTime = default)
    {
        string value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (dateTime != default)
        {
            value = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        return $"INSERT INTO update_log (updated) VALUES ('{value}')";
    }

    public static string GetActiveDealsQuery()
    {
        var query = "SELECT * FROM `deals` WHERE result LIKE '%active%'";
        return query;
    }

    public static string GetUpdateEntityQuery(long id, string tableName, Dictionary<string, object?> updatedFields)
    {
        if (!updatedFields.Any())
        {
            return string.Empty;
        }

        var setClauses = new List<string>();
        foreach (var field in updatedFields.Keys)
        {
            setClauses.Add($"`{field}` = @{field}");
        }

        string query = $"UPDATE `{tableName}` SET {string.Join(", ", setClauses)} WHERE `id` = '{id}';";

        return query;
    }

    public static string GetUpdateEntityQuery(string uniqFieldName, string uniqFieldValue, string tableName, Dictionary<string, object?> updatedFields)
    {
        if (!updatedFields.Any())
        {
            return string.Empty;
        }

        var setClauses = new List<string>();
        foreach (var field in updatedFields.Keys)
        {
            setClauses.Add($"`{field}` = @{field}");
        }

        string query = $"UPDATE `{tableName}` SET {string.Join(", ", setClauses)} WHERE `{uniqFieldName}` = '{uniqFieldValue}';";

        return query;
    }

    public static string GetLastUpdateDateFromDbLogQuery()
    {
        string query = "SELECT updated FROM `update_log` ORDER BY id DESC LIMIT 1";
        return query;
    }

    public static string GetLastIdQuery(string tableName)
    {
        string query = $"SELECT id FROM `{tableName}` ORDER BY id DESC LIMIT 1";
        return query;
    }

    public static string GetUpdateEmployeeFiredQuery(string employeeName)
    {
        string query = $"UPDATE `{Consts.TABLE_NAME_EMPLOYEE}` SET isWorking = '0', canLogin= '0' WHERE `name` = '{employeeName}';";
        return query;
    }

    public static string GetIsWorkingValueQuery(string employeeName)
    {
        string query = $"SELECT isWorking FROM `{Consts.TABLE_NAME_EMPLOYEE}` WHERE `name` = '{employeeName}'";
        return query;
    }
}
