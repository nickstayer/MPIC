using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Contractor
{
    public class AttachesInfo
    {
        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("imageFiles")]
        public List<FileObject>? ImageFiles { get; set; }

        [JsonPropertyName("imageFilesCount")]
        public int? ImageFilesCount { get; set; }

        [JsonPropertyName("audioFiles")]
        public List<FileObject>? AudioFiles { get; set; }

        [JsonPropertyName("audioFilesCount")]
        public int? AudioFilesCount { get; set; }

        [JsonPropertyName("otherFiles")]
        public List<FileObject>? OtherFiles { get; set; }

        [JsonPropertyName("otherFilesCount")]
        public int? OtherFilesCount { get; set; }
    }
}