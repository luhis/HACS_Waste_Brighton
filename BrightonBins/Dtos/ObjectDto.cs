namespace BrightonBins.Dtos;

public class ObjectDto
{
    public string ObjectType { get; init; }
    public string Guid { get; init; }

    public Dictionary<string, AttributeDto> Attributes { get; init; }

    public string Hash { get; init; }
}
