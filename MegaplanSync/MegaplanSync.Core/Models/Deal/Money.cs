using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Deal;

public class Money : IEquatable<Money>
{
    private decimal _valueInMain;
    private decimal _value;

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("valueInMain")]
    public decimal ValueInMain
    {
        get => _valueInMain;
        set => _valueInMain = Math.Round(value, 2); // Округление до 2-х знаков
    }

    [JsonPropertyName("rate")]
    public int Rate { get; set; }

    [JsonPropertyName("value")]
    public decimal Value
    {
        get => _value;
        set => _value = Math.Round(value, 2); // Округление до 2-х знаков
    }

    public bool Equals(Money? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        // Сравниваем только Value, так как это основное значение для ваших целей
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Money)obj);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}