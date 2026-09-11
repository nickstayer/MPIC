using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models.Deal;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core;

public class Logic(ILogger logger)
{
    public static DateTime ParseLogDateTime (string val)
    {
        if (string.IsNullOrWhiteSpace(val))
        {
            return default;
        }
        string format1 = "yyyy-MM-dd H:mm:ss";
        string format2 = "dd.MM.yyyy H:mm:ss";
        if (DateTime.TryParseExact(val, format1, null, System.Globalization.DateTimeStyles.None, out DateTime dateObject1))
        {
            return dateObject1;
        }
        if (DateTime.TryParseExact(val, format2, null, System.Globalization.DateTimeStyles.None, out DateTime dateObject2))
        {
            return dateObject2;
        }
        return default;
    }

    public static long GetNewestId<T>(List<T> items) where T : IHasId
    {
        if (items == null || items.Count == 0)
        {
            return 0;
        }

        var result = items
            .Select(item => long.Parse(item.Id))
            .OrderByDescending(id => id)
            .FirstOrDefault();

        return result;
    }

    public static long GetOldestId<T>(List<T> deals) where T : IHasId
    {
        var result = deals
            .Select(deal => long.Parse(deal.Id))
            .OrderBy(id => id).FirstOrDefault();
        return result;
    }

    public static bool IsTimeUpdatedDiff(Deal dealFromDb, Deal dealFromApi)
    {
        var dealFromDbTimeUpdated = dealFromDb.TimeUpdated.Value;
        var dealFromApiTimeUpdated = dealFromApi.TimeUpdated.Value;
        return IsTimeDiff(dealFromDbTimeUpdated, dealFromApiTimeUpdated);
    }

    public static bool IsTimeDiff(DateTime one, DateTime two)
    {
        return !one.Equals(two);
    }

    public static Dictionary<string, object?> GetDifferences(object original, object updated)
    {
        var differences = new Dictionary<string, object?>();
        var serializer = new JsonHelper();

        if (original == null || updated == null)
            return differences;

        var type = original.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null);

        foreach (var property in properties)
        {
            var jsonPropertyAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            var propertyName = jsonPropertyAttribute?.Name ?? property.Name;

            var originalValue = property.GetValue(original);
            var updatedValue = property.GetValue(updated);
            var propertyType = property.PropertyType;

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
            {
                string originalJson = serializer.Serialize(originalValue);
                string updatedJson = serializer.Serialize(updatedValue);

                if (originalJson != updatedJson)
                {
                    differences[propertyName] = updatedJson;
                }
                continue;
            }

            if (!Equals(originalValue, updatedValue))
            {
                if (updatedValue != null && propertyType.IsClass && propertyType != typeof(string))
                {
                    string jsonString = serializer.Serialize(updatedValue);
                    differences[propertyName] = jsonString == null ? "0" : jsonString;
                }
                else
                {
                    differences[propertyName] = updatedValue == null ? "0" : updatedValue;
                }
            }
        }

        return differences;
    }
}