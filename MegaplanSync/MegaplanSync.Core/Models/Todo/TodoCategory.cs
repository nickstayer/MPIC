using MegaplanSync.Core.Interfaces;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Todo;

public class TodoCategory : IHasId
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}
