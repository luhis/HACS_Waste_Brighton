using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrightonBins.Converter;

public class SmartValueConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Null => null,
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };
    }

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case decimal dec:
                writer.WriteNumberValue(dec);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case string s when bool.TryParse(s, out bool boolVal):
                writer.WriteBooleanValue(boolVal);
                break;
            case string s when ShouldWriteAsNumber(s):
                // Convert numeric string to number for timestamp fields
                if (long.TryParse(s, out long longVal))
                {
                    writer.WriteNumberValue(longVal);
                }
                else if (double.TryParse(s, out double doubleVal))
                {
                    writer.WriteNumberValue(doubleVal);
                }
                else
                {
                    writer.WriteStringValue(s);
                }
                break;
            // All other strings remain as strings (IDs, UPRNs, etc.)
            default:
                writer.WriteStringValue(value?.ToString());
                break;
        }
    }

    private static bool ShouldWriteAsNumber(string value)
    {
        // Only consider conversion for strings that look like numbers
        if (!long.TryParse(value, out long numericValue))
        {
            return false;
        }

        // Unix timestamps in milliseconds are typically in the range:
        // 1,000,000,000,000 (Sept 2001) to 10,000,000,000,000 (Nov 2286)
        // For HashValue objects without property name context, we assume
        // large numbers in this range are timestamps
        return numericValue is >= 1_000_000_000_000 and < 10_000_000_000_000;
    }
}
