using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Contractor
{
    public class RelationLinkMetadata
    {
        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; }
    }
}