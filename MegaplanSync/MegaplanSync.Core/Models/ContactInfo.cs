using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models;

public class ContactInfo
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("type")]
    public object Type { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; }

    [JsonPropertyName("isMain")]
    public bool? IsMain { get; set; }

    [JsonPropertyName("subject")]
    public Subject Subject { get; set; }

    [JsonIgnore]
    public AddressType AddressType
    {
        get => Type as AddressType;
        set => Type = value;
    }
}