using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models;

/// <summary>
/// Дата-время из API Мегаплана (обёртка над значением).
/// </summary>
public class DateTimeObject : IEquatable<DateTimeObject>
{
    public DateTimeObject() { }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    [JsonPropertyName("value")]
    public System.DateTime Value { get; set; }

    public bool Equals(DateTimeObject? other)
    {
        if (other == null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as DateTimeObject);
    public override int GetHashCode() => (Value).GetHashCode();
}
