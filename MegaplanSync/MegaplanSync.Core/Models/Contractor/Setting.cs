using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Contractor
{
    public class Setting
    {
        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }
}