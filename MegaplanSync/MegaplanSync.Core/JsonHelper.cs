using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models.Contractor;
using MegaplanSync.Core.Models.Deal;
using System.Data;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MegaplanSync.Core;

public class JsonHelper(ILogger logger = null)
{
    private readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string Serialize(object? updatedValue)
    {
        return JsonSerializer.Serialize(updatedValue, options);
    }

    public static void SaveEntitiesToFile<T>(List<T> entities, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        string jsonString = JsonSerializer.Serialize(entities, options);
        File.WriteAllText(filePath, jsonString);
    }

    // TODO: перейти на LoadEntitiesFromFile
    public static List<Deal> LoadDealsFromFile(string filePath)
    {
        string jsonString = File.ReadAllText(filePath);
        var deals = JsonSerializer.Deserialize<List<Deal>>(jsonString);
        var normalizer = new DataNormalizer(Consts.MAPPING_DEAL_RULES_FILE);
        foreach (var deal in deals)
        {
            normalizer.Normalize(deal);
        }
        return deals ?? [];
    }

    // TODO: перейти на LoadEntitiesFromFile
    public static List<Contractor> LoadContractorsFromFile(string filePath)
    {
        string jsonString = File.ReadAllText(filePath);
        var contractors = JsonSerializer.Deserialize<List<Contractor>>(jsonString);
        return contractors ?? [];
    }

    public List<T> LoadEntitiesFromFile<T>(string filePath)
    {
        string jsonString = File.ReadAllText(filePath);
        try
        {
            var entities = JsonSerializer.Deserialize<List<T>>(jsonString);
            return entities ?? [];
        }
        catch (Exception ex)
        {
            logger?.LogError($"Не удалось десериализовать файл {filePath}: {ex}");
            return [];
        }
    }

    public T? LoadEntityFromFile<T>(string filePath)
    {
        if (!File.Exists(filePath))
        {
            logger?.LogWarning($"Файл не найден: {filePath}");
            return default;
        }

        string jsonString = File.ReadAllText(filePath);
        try
        {
            var entity = JsonSerializer.Deserialize<T>(jsonString);
            return entity;
        }
        catch (Exception ex)
        {
            logger?.LogError($"Не удалось десериализовать файл {filePath}. Ошибка: {ex.Message}");
            return default;
        }
    }

    public T? DeserializeOrDefault<T>(DataRow row, string columnName) where T : class
    {
        if (!row.Table.Columns.Contains(columnName) || row.IsNull(columnName))
        {
            return null;
        }

        string? jsonString = row[columnName]?.ToString();

        if (string.IsNullOrWhiteSpace(jsonString) || jsonString == "0")
        {
            return null;
        }

        try
        {
            jsonString = Regex.Replace(
                jsonString,
                @"\\(?![""\\/bfnrtu])", // Ищем: \ не за которым не следует валидный символ
                @"" // Заменяем на пустую строку, убирая слэш и проблемный символ (наиболее безопасно).
            );

            jsonString = jsonString
                .Replace("\t", "\\t")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");

            return JsonSerializer.Deserialize<T>(jsonString, options);
        }
        catch (JsonException ex)
        {
            logger?.LogError($"Ошибка десериализации JSON из колонки '{columnName}' для типа '{typeof(T).Name}'. " +
                             $"JSON: '{jsonString}'. Ошибка: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            logger?.LogError($"Неожиданная ошибка при десериализации JSON из колонки '{columnName}' для типа '{typeof(T).Name}'. " +
                             $"JSON: '{jsonString}'. Ошибка: {ex.Message}");
            return null;
        }
    }

    ///// <summary>
    ///// Вспомогательный метод для безопасной десериализации JSON из колонки DataRow.
    ///// Обрабатывает DBNull, пустые строки и ошибки десериализации, возвращая null.
    ///// </summary>
    //public T? DeserializeOrDefault<T>(DataRow row, string columnName) where T : class
    //{
    //    if (!row.Table.Columns.Contains(columnName) || row.IsNull(columnName))
    //    {
    //        return null;
    //    }

    //    string? jsonString = row[columnName]?.ToString()?
    //        .Replace("\t", "\\t")
    //        .Replace("\r", "\\r")
    //        .Replace("\n", "\\n");

    //    if (string.IsNullOrWhiteSpace(jsonString) || jsonString == "0")
    //    {
    //        return null;
    //    }

    //    try
    //    {
    //        return JsonSerializer.Deserialize<T>(jsonString, options);
    //    }
    //    catch (JsonException ex)
    //    {
    //        logger?.LogError($"Ошибка десериализации JSON из колонки '{columnName}' для типа '{typeof(T).Name}'. JSON: '{jsonString}'. Ошибка: {ex.Message}");
    //        return null;
    //    }
    //    catch (Exception ex)
    //    {
    //        logger?.LogError($"Неожиданная ошибка при десериализации JSON из колонки '{columnName}' для типа '{typeof(T).Name}'. JSON: '{jsonString}'. Ошибка: {ex.Message}");
    //        return null;
    //    }
    //}

    /// <summary>
    /// Вспомогательный метод для безопасной десериализации JSON-массива из колонки DataRow в List<T>.
    /// Обрабатывает DBNull, пустые строки и ошибки десериализации, возвращая пустой список.
    /// </summary>
    public List<T>? DeserializeListOrDefault<T>(DataRow row, string columnName) where T : class
    {
        if (!row.Table.Columns.Contains(columnName) || row.IsNull(columnName))
        {
            return null;
        }

        string? jsonString = row[columnName]?.ToString();
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return null;
        }

        try
        {
            if (jsonString == "0" || jsonString == "[]") return null;
            var list = JsonSerializer.Deserialize<List<T>>(jsonString, options);
            return list;
        }
        catch (JsonException ex)
        {
            logger?.LogError($"Ошибка десериализации JSON-массива из колонки '{columnName}' для типа 'List<{typeof(T).Name}>'. JSON: '{jsonString}'. Ошибка: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            logger?.LogError($"Неожиданная ошибка при десериализации JSON-массива из колонки '{columnName}' для типа 'List<{typeof(T).Name}>'. JSON: '{jsonString}'. Ошибка: {ex.Message}");
            return null;
        }
    }

    public object? DeserializeOrDefault(DataRow row, string columnName, Type type)
    {
        if (!row.Table.Columns.Contains(columnName) || row.IsNull(columnName))
        {
            return null;
        }

        string? jsonString = row[columnName]?.ToString()?
            .Replace("\t", "\\t")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");

        if (string.IsNullOrWhiteSpace(jsonString) || jsonString == "0")
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(jsonString, type, options);
        }
        catch (JsonException ex)
        {
            logger?.LogError($"Ошибка десериализации JSON из колонки '{columnName}' для типа '{type.Name}'. JSON: '{jsonString}'. Ошибка: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            logger?.LogError($"Неожиданная ошибка при десериализации JSON из колонки '{columnName}' для типа '{type.Name}'. JSON: '{jsonString}'. Ошибка: {ex.Message}");
            return null;
        }
    }

    // Универсальный метод для списков
    public object? DeserializeListOrDefault(DataRow row, string columnName, Type type)
    {
        return DeserializeOrDefault(row, columnName, type);
    }

    /// <summary>
    /// Вспомогательный метод для безопасной сериализации объекта в JSON-строку или возврата DBNull.Value, если объект null.
    /// </summary>
    public object SerializeOrNull<T>(T? obj) where T : class
    {
        if (obj == null)
        {
            return 0;
        }
        try
        {
            return JsonSerializer.Serialize(obj, options);
        }
        catch (Exception ex)
        {
            logger?.LogError($"Ошибка сериализации объекта типа {typeof(T).Name}: {ex.Message}", ex);
            return 0;
        }
    }

    /// <summary>
    /// Вспомогательный метод для безопасной сериализации Nullable значимого типа в JSON-строку
    /// или возврата DBNull.Value, если значение null.
    /// </summary>
    public object SerializeNullableValueOrNull<T>(T? obj) where T : struct
    {
        if (obj == null)
        {
            return 0;
        }
        try
        {
            return JsonSerializer.Serialize(obj.Value, options);
        }
        catch (Exception ex)
        {
            logger?.LogError($"Ошибка сериализации Nullable значимого объекта типа {typeof(T).Name}: {ex.Message}", ex);
            return 0;
        }
    }
}
