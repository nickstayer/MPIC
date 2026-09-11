using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models
{
    public class DateOnlyObject : IEquatable<DateOnlyObject>
    {
        [JsonPropertyName("contentType")]
        public string ContentType { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("day")]
        public int Day { get; set; }

        public bool Equals(DateOnlyObject? other)
        {
            if (other == null) return false;
            return Year == other.Year && Month == other.Month && Day == other.Day;
        }

        public override bool Equals(object obj) => Equals(obj as DateOnlyObject);
        public override int GetHashCode() => (Year, Month, Day).GetHashCode();
    }
}
