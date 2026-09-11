using System.Text.Json;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models;

public class Meta
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new List<string>();

    [JsonPropertyName("pagination")]
    public JsonElement PaginationElement { get; set; }

    [JsonIgnore]
    public object Pagination
    {
        get
        {
            if (PaginationElement.ValueKind == JsonValueKind.Object)
            {
                return PaginationElement.Deserialize<Pagination>();
            }
            else if (PaginationElement.ValueKind == JsonValueKind.Array)
            {
                return PaginationElement.Deserialize<List<Pagination>>();
            }
            return null;
        }
    }
}
