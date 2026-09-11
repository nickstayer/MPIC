using MegaplanSync.Core;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models.Deal;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MegaplanSync.DataAccess.MySql;

public class ParametersHelper(ILogger logger)
{
    public Dictionary<string, object> GetParameters<T>(T obj, Dictionary<string, object> additionalParameters = null)
    {
        var serializer = new JsonHelper();
        var parameters = new Dictionary<string, object>();

        //var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
            .ToList();

        foreach (var property in properties)
        {
            var jsonPropertyAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            var parameterName = jsonPropertyAttribute?.Name ?? property.Name;
            var value = property.GetValue(obj);
            var propertyType = property.PropertyType;

            if (propertyType == typeof(bool?))
            {
                parameters[parameterName] = BoolToIntOrDbNull((bool?)value);
            }
            else if (propertyType.IsPrimitive || propertyType == typeof(string) || Nullable.GetUnderlyingType(propertyType)?.IsPrimitive == true)
            {
                parameters[parameterName] = (object?)value ?? "0";
            }
            else
            {
                parameters[parameterName] = serializer.SerializeOrNull(value);
            }
        }

        if (additionalParameters != null)
        {
            foreach (var pair in additionalParameters)
            {
                parameters[pair.Key] = pair.Value;
            }
        }

        return parameters;
    }

    /// <summary>
    /// Вспомогательный метод для преобразования bool? в int (1/0) или DBNull.Value.
    /// </summary>
    private object BoolToIntOrDbNull(bool? value)
    {
        if (value == null)
        {
            return 0;
        }
        return value.Value ? 1 : 0;
    }
}
