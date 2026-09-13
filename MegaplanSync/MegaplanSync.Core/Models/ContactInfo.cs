using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models;

/// <summary>
/// Контактные данные контрагента. MPIC использует только Value (email).
/// </summary>
public class ContactInfo
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
