using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Contractor
{
    public class InteractionCounter
    {
        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("count")]
        public int? Count { get; set; }
    }
}