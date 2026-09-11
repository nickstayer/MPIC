using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Employee;

public class EmployeeStatus
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("masterType")]
    public string MasterType { get; set; }
}
