using System.Text.Json;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Deal;

public class NullableBoolConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return false;
        }

        if (reader.TokenType == JsonTokenType.True)
        {
            return true;
        }

        if (reader.TokenType == JsonTokenType.False)
        {
            return false;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            if (reader.GetString() == "1")
            {
                return true;
            }
            if (reader.GetString() == "0")
            {
                return false;
            }
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value ?? false);
    }
}
