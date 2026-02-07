using System.Text.Json.Serialization;

namespace BrightonBins.Dtos;

public class AttributeDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Readonly { get; init; }
    public string? Value { get; init; }
}