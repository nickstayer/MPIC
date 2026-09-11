using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models.Deal;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Contractor;

public class Contractor : IHasId
{
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("middleName")]
    public string? MiddleName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("contactInfo")]
    public List<ContactInfo>? ContactInfo { get; set; }

    public bool Equals(Contractor? other)
    {
        if (other == null) return false;
        return Id == other.Id;
    }

    public override bool Equals(object obj) => Equals(obj as Contractor);
    public override int GetHashCode() => (Id).GetHashCode();

}