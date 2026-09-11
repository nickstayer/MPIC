using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Deal
{
    public class RefLinkField
    {
        [JsonPropertyName("contentType")]
        public string ContentType { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("hrName")]
        public string HrName { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("refContentType")]
        public List<string> RefContentType { get; set; }
    }
}
