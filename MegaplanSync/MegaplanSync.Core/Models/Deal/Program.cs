using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Deal;

public class Program
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("roles")]
    public List<ProgramRole> Roles { get; set; }

    [JsonPropertyName("possibleActions")]
    public List<string> PossibleActions { get; set; }
}
