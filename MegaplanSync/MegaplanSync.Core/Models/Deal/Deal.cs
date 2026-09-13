using MegaplanSync.Core.Interfaces;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Deal;

/// <summary>
/// Сделка Мегаплана. Оставлены только поля, необходимые MPIC:
/// идентификация, контрагент (для сопоставления email) и время создания/обновления.
/// </summary>
public class Deal : IHasId
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("contractor")]
    public Contractor.Contractor? Contractor { get; set; }

    [JsonPropertyName("timeCreated")]
    public DateTimeObject? TimeCreated { get; set; }

    [JsonPropertyName("timeUpdated")]
    public DateTimeObject? TimeUpdated { get; set; }
}
