using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models;

public class Response<T>
{
    [JsonPropertyName("meta")]
    public Meta Meta { get; set; }

    [JsonPropertyName("data")]
    public T Data { get; set; }
}

public class Responses<T>
{
    [JsonPropertyName("meta")]
    public Meta Meta { get; set; }

    [JsonPropertyName("data")]
    public List<T> Data { get; set; }
}
