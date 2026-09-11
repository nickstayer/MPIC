using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Contractor
{
    public class RelationLink
    {
        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("placeholder")]
        public string? Placeholder { get; set; }

        [JsonPropertyName("relationType")]
        public string? RelationType { get; set; }

        [JsonPropertyName("representation")]
        public string? Representation { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; }

        [JsonPropertyName("subject")]
        public BaseEntity? Subject { get; set; }

        [JsonPropertyName("origin")]
        public string? Origin { get; set; }

        [JsonPropertyName("target")]
        public BaseEntity? Target { get; set; }

        [JsonPropertyName("isTargetVisible")]
        public bool? IsTargetVisible { get; set; }

        [JsonPropertyName("isSubjectReadableForMentionUser")]
        public bool? IsSubjectReadableForMentionUser { get; set; }

        [JsonPropertyName("metadata")]
        public RelationLinkMetadata? Metadata { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    public class BaseEntity
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}