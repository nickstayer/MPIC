using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Contractor
{
    public class Country
    {
        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("isDefault")]
        public bool? IsDefault { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}