using System.Text.Json.Serialization;

namespace BrightonBins.Dtos;

public class ObjectDto
{
    public required string ObjectType { get; init; }
    public required string Guid { get; init; }

    [JsonConverter(typeof(Converter.AttributesDictionaryConverter))]
    public required Dictionary<string, AttributeDto> Attributes { get; init; }

    public string? Hash { get; init; }
}
