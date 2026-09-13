using MegaplanSync.Core.Interfaces;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Contractor;

/// <summary>
/// Контрагент сделки. MPIC использует только контактную информацию (email).
/// </summary>
public class Contractor : IHasId
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("contactInfo")]
    public List<ContactInfo>? ContactInfo { get; set; }
}
