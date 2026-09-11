using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models;

public class IntervalTime
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = "IntervalTime";

    [JsonPropertyName("from")]
    public DateTimeObject From { get; set; }

    [JsonPropertyName("to")]
    public DateTimeObject To { get; set; }
}
