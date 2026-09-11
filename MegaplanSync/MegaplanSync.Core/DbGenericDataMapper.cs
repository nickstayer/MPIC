using MegaplanSync.Core.Interfaces;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core;

public class DbGenericDataMapper<T>(ILogger logger) where T : class, new()
{
    private readonly ILogger _logger = logger;
    private readonly JsonHelper _jsonHelper = new JsonHelper(logger);

    public List<T> Map(DataTable dataTable, DataNormalizer normalizer = null)
    {
        var result = new List<T>();

        if (dataTable == null || dataTable.Rows.Count == 0)
        {
            _logger?.LogWarning("Получена пустая или null DataTable для маппинга.");
            return result;
        }

        foreach (DataRow row in dataTable.Rows)
        {
            var entity = MapFromDataRow(row);
            if (entity != null)
            {
                normalizer?.Normalize<T>(entity);
                result.Add(entity);
            }
        }

        return result;
    }

    private T? MapFromDataRow(DataRow row)
    {
        var entity = new T();
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            var columnName = GetColumnNameForProperty(property);

            if (columnName != null && row.Table.Columns.Contains(columnName))
            {
                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                try
                {
                    // Обработка сложных типов (вложенных объектов и списков)
                    if (IsCustomType(targetType) || IsGenericList(targetType))
                    {
                        var deserializedValue = _jsonHelper.DeserializeOrDefault(row, columnName, property.PropertyType);
                        property.SetValue(entity, deserializedValue);
                    }
                    // Обработка bool из "1" или "0"
                    else if (targetType == typeof(bool))
                    {
                        property.SetValue(entity, GetBoolFromDbValue(row, columnName));
                    }
                    // Конвертация остальных примитивных типов
                    else
                    {
                        if (row.IsNull(columnName))
                        {
                            property.SetValue(entity, null);
                        }
                        else
                        {
                            var value = row[columnName];
                            if (targetType == typeof(string) && value.ToString() == "0")
                            {
                                property.SetValue(entity, null);
                            }
                            else
                            {
                                property.SetValue(entity, Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Не удалось установить значение для свойства '{property.Name}' из колонки '{columnName}'. Ошибка: {ex.Message}");
                    return null; // Возвращаем null, если не удалось создать сущность
                }
            }
        }
        return entity;
    }

    private string? GetColumnNameForProperty(PropertyInfo property)
    {
        var jsonPropertyNameAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        return jsonPropertyNameAttribute?.Name;
    }

    private bool GetBoolFromDbValue(DataRow row, string columnName)
    {
        if (row.Table.Columns.Contains(columnName) && !row.IsNull(columnName))
        {
            return row[columnName]?.ToString() == "1";
        }
        return false;
    }

    private bool IsCustomType(Type type)
    {
        return type.IsClass && type.Namespace != null && type.Namespace.StartsWith("MegaplanSync.Core.Models") && type != typeof(string);
    }

    private bool IsGenericList(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
    }
}