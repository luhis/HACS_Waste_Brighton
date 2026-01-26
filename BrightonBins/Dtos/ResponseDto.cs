namespace BrightonBins.Dtos;

public class ResponseDto
{
    public Dictionary<long, Dictionary<string, HashValue>> Changes { get; init; }
    public ObjectDto[] Objects { get; init; }

    public string CsrfToken { get; init; }
}

public class ObjectDto
{
    public string ObjectType { get; init; }
    public string Guid { get; init; }

    public Dictionary<string, AttributeDto> Attributes { get; init; }

    public string Hash { get; init; }
}

public class AttributeDto
{
    public string Readonly { get; init; }
    public string Value { get; init; }
}