using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Deal;

public class Tag
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
