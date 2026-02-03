namespace BrightonBins.Dtos;

public class ResponseDto
{
    public required Dictionary<long, Dictionary<string, HashValue>> Changes { get; init; }

    public required ObjectDto[] Objects { get; init; }

    public string? CsrfToken { get; init; }
}
