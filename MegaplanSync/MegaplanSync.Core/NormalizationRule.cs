using System.Text.Json;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core;

/// <summary>
/// Правило нормализации: заменяет null/пустое значение свойства на NullEquivalent.
/// </summary>
public class NormalizationRule
{
    public string ClassPropertyName { get; set; } = string.Empty;
    public string ClassPropertyType { get; set; } = string.Empty;
    public JsonElement NullEquivalent { get; set; }
}
