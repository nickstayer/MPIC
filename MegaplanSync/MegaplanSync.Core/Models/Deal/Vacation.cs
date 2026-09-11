using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Deal;

public class Vacation
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("employee")]
    public Subject Employee { get; set; }

    [JsonPropertyName("firstDay")]
    public DateOnlyObject FirstDay { get; set; }

    [JsonPropertyName("lastDay")]
    public DateOnlyObject LastDay { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("isFavorite")]
    public bool IsFavorite { get; set; }
}
