using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models;

public class Address
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("type")]
    public AddressType Type { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("isMain")]
    public bool? IsMain { get; set; }
}
