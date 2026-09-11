using MegaplanSync.Core.Interfaces;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Todo;

public class Todo : IHasId
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("status")]
    public TodoStatus Status { get; set; }

    [JsonPropertyName("category")]
    public TodoCategory Category { get; set; }

    [JsonPropertyName("timeFinished")]
    public DateTimeObject TimeFinished { get; set; }

    [JsonPropertyName("timeCreated")]
    public DateTimeObject TimeCreated { get; set; }

    [JsonPropertyName("when")]
    public IntervalTime When { get; set; }

    [JsonPropertyName("responsible")]
    public Employee.Employee Responsible { get; set; }

    [JsonPropertyName("isDropped")]
    public bool IsDropped { get; set; }

    [JsonPropertyName("isOverdue")]
    public bool IsOverdue { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}
