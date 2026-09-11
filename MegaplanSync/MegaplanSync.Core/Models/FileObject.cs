using System.Text.Json;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models;

public class FileObject : IEquatable<FileObject>
{
    public bool Equals(FileObject? other)
    {
        if (other == null) return false;
        return Id == other.Id;
    }

    public override bool Equals(object obj) => Equals(obj as FileObject);
    public override int GetHashCode() => (Id).GetHashCode();

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; }

    [JsonPropertyName("extension")]
    public string Extension { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("subject")]
    public Subject Subject { get; set; }

    [JsonPropertyName("timeCreated")]
    public TimeCreated TimeCreated { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("possibleActions")]
    public List<string> PossibleActions { get; set; }

    [JsonPropertyName("metadata")]
    public object Metadata { get; set; }

    [JsonPropertyName("thumbnail")]
    public string Thumbnail { get; set; }
}

public class TimeCreated
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("value")]
    [JsonConverter(typeof(DateTimeJsonConverter))] // Кастомный конвертер для DateTime
    public DateTime Value { get; set; }
}

// Кастомный конвертер для DateTime (если нужно особое поведение при парсинге)
public class DateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("o")); // ISO 8601 format
    }
}
