using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models;

public class AddressType
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
