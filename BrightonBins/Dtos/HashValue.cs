using System.Text.Json.Serialization;

namespace BrightonBins.Dtos;

public class HashValue
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hash { get; init; }
    public required string Value { get; init; }
}