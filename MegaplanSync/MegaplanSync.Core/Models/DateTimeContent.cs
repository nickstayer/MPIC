using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models;

public class DateTimeContent
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("value")]
    public System.DateTime? Value { get; set; }
    public bool Equals(DateTimeContent? other)
    {
        if (other == null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object obj) => Equals(obj as DateTimeContent);
    public override int GetHashCode() => (Value).GetHashCode();
}