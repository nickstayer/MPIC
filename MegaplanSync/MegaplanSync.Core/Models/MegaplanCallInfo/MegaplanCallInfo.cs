using MegaplanSync.Core.Interfaces;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.MegaplanCallInfo;

public class MegaplanCallInfo : IHasId
{
    public MegaplanCallInfo() { }

    [JsonPropertyName("fromPhone")]
    public string FromPhone { get; set; }

    [JsonPropertyName("toPhone")]
    public string ToPhone { get; set; }

    [JsonPropertyName("fromUser")]
    public Contractor.Contractor FromUser { get; set; }

    [JsonPropertyName("toUser")]
    public Contractor.Contractor ToUser { get; set; }

    [JsonPropertyName("providerId")]
    public object ProviderId { get; set; }

    [JsonPropertyName("timeFrom")]
    public DateTimeObject TimeFrom { get; set; }

    [JsonPropertyName("timeTo")]
    public DateTimeObject TimeTo { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("recordLinks")]
    public List<string> RecordLinks { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    public bool Equals(MegaplanCallInfo? other)
    {
        if (other == null) return false;
        return Id == other.Id;
    }

    public override bool Equals(object obj) => Equals(obj as MegaplanCallInfo);
    public override int GetHashCode() => (Id).GetHashCode();
}
