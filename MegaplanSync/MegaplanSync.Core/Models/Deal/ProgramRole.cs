using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Deal;

public class ProgramRole
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("linkedField")]
    public RefLinkField LinkedField { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("isMultiple")]
    public bool IsMultiple { get; set; }
}
