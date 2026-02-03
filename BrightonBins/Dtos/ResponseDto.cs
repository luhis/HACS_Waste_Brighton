namespace BrightonBins.Dtos;

public class ResponseDto
{
    public Dictionary<long, Dictionary<string, HashValue>> Changes { get; init; }

    public ObjectDto[] Objects { get; init; }

    public string CsrfToken { get; init; }
}
