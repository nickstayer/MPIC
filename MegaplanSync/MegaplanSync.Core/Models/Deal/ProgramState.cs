using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Deal;

public class ProgramState : IEquatable<ProgramState>
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("entryPointName")]
    public string EntryPointName { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("isEntry")]
    public bool IsEntry { get; set; }

    public bool Equals(ProgramState? other)
    {
        if (other == null) return false;
        return Id == other.Id;
    }

    public override bool Equals(object obj) => Equals(obj as ProgramState);
    public override int GetHashCode() => (Id).GetHashCode();
}
