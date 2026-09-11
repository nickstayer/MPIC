using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Deal;

public class Owner
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}
