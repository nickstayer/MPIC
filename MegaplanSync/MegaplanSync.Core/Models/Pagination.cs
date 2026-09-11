using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models;

public class Pagination
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("perPage")]
    public int PerPage { get; set; }

    [JsonPropertyName("currentPage")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("hasMoreNext")]
    public bool HasMoreNext { get; set; }

    [JsonPropertyName("hasMorePrev")]
    public bool HasMorePrev { get; set; }
}
