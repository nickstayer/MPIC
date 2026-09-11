using MegaplanSync.Core.Models.Deal;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models;

public class Comment
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("isDropped")]
    public bool IsDropped { get; set; }

    [JsonPropertyName("subject")]
    public Subject Subject { get; set; }

    [JsonPropertyName("owner")]
    public Owner Owner { get; set; }

    [JsonPropertyName("attaches")]
    public List<FileObject> Attaches { get; set; }

    [JsonPropertyName("timeCreated")]
    public DateTimeContent TimeCreated { get; set; }
}
