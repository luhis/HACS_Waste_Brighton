using System.Text.Json;
using System.Text.Json.Serialization;
using BrightonBins.Dtos;

namespace BrightonBins.Converter;

public class AttributesDictionaryConverter : JsonConverter<Dictionary<string, AttributeDto>>
{
    private static readonly string[] TimestampFieldPatterns = 
    [
        "date",           // changedDate, createdDate, DateCreated
        "time",           // TimeInMinutes (though this might be a decimal)
        "parse"           // collectionDateParse
    ];

    public override Dictionary<string, AttributeDto>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Use default deserialization
        return JsonSerializer.Deserialize<Dictionary<string, AttributeDto>>(ref reader, 
            new JsonSerializerOptions(options) { Converters = { } });
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, AttributeDto> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var (key, attributeDto) in value)
        {
            writer.WritePropertyName(key);
            
            writer.WriteStartObject();

            // Write readonly property if present
            if (attributeDto.Readonly.HasValue)
            {
                writer.WriteBoolean("readonly", attributeDto.Readonly.Value);
            }

            // Write value property with smart type conversion
            writer.WritePropertyName("value");
            WriteSmartValue(writer, attributeDto.Value, key);

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WriteSmartValue(Utf8JsonWriter writer, object? value, string propertyName)
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
            case string s when ShouldConvertToNumber(s, propertyName):
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
            default:
                writer.WriteStringValue(value?.ToString());
                break;
        }
    }

    private static bool ShouldConvertToNumber(string value, string propertyName)
    {
        // Only consider conversion for strings that look like numbers
        if (!long.TryParse(value, out long numericValue))
        {
            return false;
        }

        // Check if the property name matches timestamp patterns
        var lowerPropertyName = propertyName.ToLowerInvariant();
        var isTimestampField = TimestampFieldPatterns.Any(pattern => 
            lowerPropertyName.Contains(pattern, StringComparison.OrdinalIgnoreCase));

        if (!isTimestampField)
        {
            return false;
        }

        // Unix timestamps in milliseconds are typically in the range:
        // 1,000,000,000,000 (Sept 2001) to 10,000,000,000,000 (Nov 2286)
        // This helps distinguish timestamps from large IDs
        return numericValue is >= 1_000_000_000_000 and < 10_000_000_000_000;
    }
}
