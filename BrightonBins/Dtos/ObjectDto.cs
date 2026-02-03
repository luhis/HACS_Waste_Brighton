namespace BrightonBins.Dtos;

public class ObjectDto
{
    public required string ObjectType { get; init; }
    public required string Guid { get; init; }

    public required Dictionary<string, AttributeDto> Attributes { get; init; }

    public string? Hash { get; init; }
}
