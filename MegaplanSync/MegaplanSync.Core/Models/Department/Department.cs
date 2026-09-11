using MegaplanSync.Core.Interfaces;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Department;

public class Department : IHasId
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    public bool Equals(Department? other)
    {
        if (other == null) return false;
        return Id == other.Id;
    }

    public override bool Equals(object obj) => Equals(obj as Department);
    public override int GetHashCode() => (Id).GetHashCode();
}
