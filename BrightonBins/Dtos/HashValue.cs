using BrightonBins.Converter;
using System.Text.Json.Serialization;

namespace BrightonBins.Dtos;

public class HashValue
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hash { get; init; }
    
    [JsonConverter(typeof(SmartValueConverter))]
    public required object Value { get; init; }
}