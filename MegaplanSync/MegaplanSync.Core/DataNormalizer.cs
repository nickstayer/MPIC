using MegaplanSync.Core.Interfaces;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core;

public class DataNormalizer
{
    private readonly Dictionary<string, NormalizationRule> _rules;
    private ILogger _logger;
    public DataNormalizer(string rulesFile, ILogger logger = null)
    {
        _rules = LoadRules(rulesFile);
        _logger = logger;
    }

    public void Normalize<T>(T obj) where T : class
    {
        if (obj == null) return;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var propertyName = property.Name;

            if (_rules.ContainsKey(propertyName))
            {
                var rule = _rules[propertyName];
                var currentValue = property.GetValue(obj);

                if(propertyName == "MiddleName")
                {
                    if(currentValue == "")
                    {

                    }
                }

                // Определяем, нужно ли применять правило NullEquivalent
                bool treatAsNull = currentValue == null;

                // Если это строковое свойство и оно не null, проверяем на пустую строку
                if (!treatAsNull && property.PropertyType == typeof(string))
                {
                    if (string.IsNullOrEmpty(currentValue as string))
                    {
                        treatAsNull = true;
                    }
                }

                if (treatAsNull)
                {
                    switch (rule.NullEquivalent.ValueKind)
                    {
                        case JsonValueKind.Number:
                            if (property.PropertyType == typeof(int?) || property.PropertyType == typeof(double?))
                            {
                                if (property.PropertyType == typeof(int?))
                                {
                                    property.SetValue(obj, rule.NullEquivalent.GetInt32());
                                }
                                else if (property.PropertyType == typeof(double?))
                                {
                                    property.SetValue(obj, rule.NullEquivalent.GetDouble());
                                }
                            }
                            break;
                        case JsonValueKind.False:
                            if (property.PropertyType == typeof(bool?))
                            {
                                property.SetValue(obj, false);
                            }
                            break;
                        case JsonValueKind.String:
                            if (property.PropertyType == typeof(string))
                            {
                                // Если пустое значение заменяется строкой, 
                                // мы все равно применяем правило
                                property.SetValue(obj, rule.NullEquivalent.GetString());
                            }
                            break;
                        case JsonValueKind.Array:
                            if (property.PropertyType.IsGenericType && typeof(List<>).IsAssignableFrom(property.PropertyType.GetGenericTypeDefinition()))
                            {
                                property.SetValue(obj, Activator.CreateInstance(property.PropertyType));
                            }
                            break;
                        case JsonValueKind.Null:
                            property.SetValue(obj, null);
                            break;
                    }
                }
            }
        }
    }

    //public void Normalize<T>(T obj) where T : class
    //{
    //    if (obj == null) return;

    //    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    //    foreach (var property in properties)
    //    {
    //        var propertyName = property.Name;



    //        if (_rules.ContainsKey(propertyName))
    //        {
    //            var rule = _rules[propertyName];
    //            var currentValue = property.GetValue(obj);

    //            if (currentValue == null)
    //            {
    //                switch (rule.NullEquivalent.ValueKind)
    //                {
    //                    case JsonValueKind.Number:
    //                        if (property.PropertyType == typeof(int?) || property.PropertyType == typeof(double?))
    //                        {
    //                            if (property.PropertyType == typeof(int?))
    //                            {
    //                                property.SetValue(obj, rule.NullEquivalent.GetInt32());
    //                            }
    //                            else if (property.PropertyType == typeof(double?))
    //                            {
    //                                property.SetValue(obj, rule.NullEquivalent.GetDouble());
    //                            }
    //                        }
    //                        break;
    //                    case JsonValueKind.False:
    //                        if (property.PropertyType == typeof(bool?))
    //                        {
    //                            property.SetValue(obj, false);
    //                        }
    //                        break;
    //                    case JsonValueKind.String:
    //                        if (property.PropertyType == typeof(string))
    //                        {
    //                            property.SetValue(obj, rule.NullEquivalent.GetString());
    //                        }
    //                        break;
    //                    case JsonValueKind.Array:
    //                        if (property.PropertyType.IsGenericType && typeof(List<>).IsAssignableFrom(property.PropertyType.GetGenericTypeDefinition()))
    //                        {
    //                            property.SetValue(obj, Activator.CreateInstance(property.PropertyType));
    //                        }
    //                        break;
    //                }
    //            }
    //        }
    //    }
    //}

    private Dictionary<string, NormalizationRule> LoadRules(string rulesFile)
    {
        if (!File.Exists(rulesFile))
        {
            _logger?.LogError($"Ошибка: Файл правил нормализации не найден по пути: {rulesFile}");
            return new Dictionary<string, NormalizationRule>();
        }

        try
        {
            string jsonString = File.ReadAllText(rulesFile);
            var rulesList = JsonSerializer.Deserialize<List<NormalizationRule>>(jsonString);

            var rulesDictionary = rulesList.ToDictionary(
                rule => rule.ClassPropertyName,
                rule => rule
            );

            return rulesDictionary;
        }
        catch (JsonException ex)
        {
            _logger?.LogError($"Ошибка десериализации JSON: {ex.Message}");
            return new Dictionary<string, NormalizationRule>();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Произошла непредвиденная ошибка при чтении файла: {ex.Message}");
            return new Dictionary<string, NormalizationRule>();
        }
    }
}