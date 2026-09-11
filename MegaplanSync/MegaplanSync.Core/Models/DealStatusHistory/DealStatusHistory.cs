using MegaplanSync.Core.Models.Deal;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.DealStatusHistory;

public class DealStatusHistory
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = "DealStatusHistory";

    [JsonPropertyName("fromStatus")]
    public ProgramState FromStatus { get; set; }

    [JsonPropertyName("toStatus")]
    public ProgramState ToStatus { get; set; }

    [JsonPropertyName("fromStatusUpdated")]
    public DateTimeObject FromStatusUpdatedDate { get; set; }

    [JsonPropertyName("toStatusUpdated")]
    public DateTimeObject ToStatusUpdatedDate { get; set; }

    [JsonPropertyName("fromStatusUser")]
    public Employee.Employee FromStatusUser { get; set; }

    [JsonPropertyName("toStatusUser")]
    public Employee.Employee ToStatusUser { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}
