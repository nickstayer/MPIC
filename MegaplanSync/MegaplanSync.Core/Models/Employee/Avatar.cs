using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Employee;

public class Avatar
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("extension")]
    public string Extension { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("thumbnail")]
    public string Thumbnail { get; set; }
}
